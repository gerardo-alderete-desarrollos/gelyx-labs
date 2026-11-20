namespace GelyxLabs.Api.Services.Email;

using GelyxLabs.Application.Abstractions.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly IHostEnvironment _env;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "{Environment} email suppressed for {To}; template locale is {Locale}",
            _env.IsDevelopment() ? "Development" : "Non-development",
            message.To,
            message.Locale);
        return Task.CompletedTask;
    }
}
