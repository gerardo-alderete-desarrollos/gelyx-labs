using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Settings;
using GelyxLabs.Application.Abstractions.Modules;
using GelyxLabs.Application.Features.Audit;
using GelyxLabs.Application.Features.Permissions;
using GelyxLabs.Application.Features.Users;
using GelyxLabs.Application.Features.Roles;
using GelyxLabs.Application.Features.Modules;
using GelyxLabs.Application.Features.Projects;
using GelyxLabs.Application.Features.Leads;
using GelyxLabs.Application.Features.Auth.Sessions;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Infrastructure.Persistence.Interceptors;
using GelyxLabs.Infrastructure.Services;

namespace GelyxLabs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 10;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IModuleSynchronizer, ModuleSynchronizer>();
        services.AddScoped<IModuleCatalog, ModuleCatalog>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionsRepository, PermissionsRepository>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IRolesRepository, RolesRepository>();
        services.AddScoped<IModulesRepository, ModulesRepository>();
        services.AddScoped<IProjectsRepository, ProjectsRepository>();
        services.AddScoped<ILeadsRepository, LeadsRepository>();
        services.AddScoped<ISessionsRepository, SessionsRepository>();
        services.AddHostedService<StartupInitializer>();

        return services;
    }
}
