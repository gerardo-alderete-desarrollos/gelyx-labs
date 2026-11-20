using Asp.Versioning;
using System.Net;
using System.Text;
using GelyxLabs.Api;
using GelyxLabs.Api.Extensions;
using GelyxLabs.Api.Middleware;
using GelyxLabs.Api.Services;
using GelyxLabs.Api.Authorization;
using GelyxLabs.Application;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;
using Serilog;

using DotNetEnv;

Env.Load(Path.Combine(AppContext.BaseDirectory, "..", "..", ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var corsOptions = CorsOptions.FromConfiguration(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.AddOptions<CorsOptions>()
    .Configure(options => options.AllowedOrigins = corsOptions.AllowedOrigins)
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<CorsOptions>, CorsOptionsValidator>();

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

builder.Services.AddOptions<GelyxLabs.Application.Options.AuthOptions>()
    .BindConfiguration(GelyxLabs.Application.Options.AuthOptions.SectionName);

builder.Services.AddOptions<GelyxLabs.Application.Options.AppOptions>()
    .BindConfiguration(GelyxLabs.Application.Options.AppOptions.SectionName);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOptions.AllowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            CorrelationIdMiddleware.GetCorrelationId(context.HttpContext));
builder.Services.AddOpenApi();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.FindFirst("purpose")?.Value == "two_factor")
                {
                    context.Fail("Temporary two-factor tokens cannot authenticate requests.");
                }
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .PostConfigure<IOptions<JwtOptions>>((options, configuredJwtOptions) =>
    {
        var jwtOptions = configuredJwtOptions.Value;
        options.TokenValidationParameters.IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
        options.TokenValidationParameters.ValidIssuer = jwtOptions.Issuer;
        options.TokenValidationParameters.ValidAudience = jwtOptions.Audience;
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }
        return ValueTask.CompletedTask;
    };

    var isTesting = builder.Environment.IsEnvironment("Testing");

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isTesting ? 10000 : 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("refresh", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isTesting ? 10000 : 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddEndpointGroups();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
    .WithTags("Health");

var version = new ApiVersion(1, 0);
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(version)
    .Build();

var versionedApi = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet);
versionedApi.MapToApiVersion(version);
app.MapEndpointGroups(versionedApi);
if (app.Environment.IsEnvironment("Testing"))
{
    versionedApi.MapGet("permission-test", () => Results.Ok())
        .RequirePermission("users.read");
    versionedApi.MapGet("exception-test", ThrowTestException);
}

app.Run();

static IResult ThrowTestException() => throw new InvalidOperationException("Sensitive test exception.");

public partial class Program { }
