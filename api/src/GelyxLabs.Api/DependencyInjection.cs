namespace GelyxLabs.Api;

using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Api.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<ITokenService, Services.Identity.TokenService>();
        services.AddScoped<IRefreshTokenService, Services.Identity.RefreshTokenService>();
        IConfiguration emailConfiguration = (IConfiguration?)configuration?.GetSection(EmailOptions.SectionName)
            ?? new ConfigurationBuilder().Build();
        services.AddOptions<EmailOptions>()
            .Bind(emailConfiguration)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddHttpClient<ResendEmailSender>(client => client.BaseAddress = new Uri("https://api.resend.com/"));
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<LoggingEmailSender>();
        services.AddScoped<IEmailSender>(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider;
            return provider.ToLowerInvariant() switch
            {
                "resend" => serviceProvider.GetRequiredService<ResendEmailSender>(),
                "smtp" => serviceProvider.GetRequiredService<SmtpEmailSender>(),
                _ => serviceProvider.GetRequiredService<LoggingEmailSender>()
            };
        });
        services.AddSingleton<EmailQueueService>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueueService>());
        services.AddHostedService(sp => sp.GetRequiredService<EmailQueueService>());
        return services;
    }
}
