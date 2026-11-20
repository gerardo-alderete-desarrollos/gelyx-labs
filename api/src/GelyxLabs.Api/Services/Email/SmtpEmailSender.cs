namespace GelyxLabs.Api.Services.Email;

using System.Net;
using System.Net.Mail;
using GelyxLabs.Application.Abstractions.Email;
using Microsoft.Extensions.Options;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options) => _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.SmtpHost!, _options.SmtpPort)
        {
            EnableSsl = _options.SmtpUseSsl
        };
        if (!string.IsNullOrWhiteSpace(_options.SmtpUserName))
        {
            client.Credentials = new NetworkCredential(_options.SmtpUserName, _options.SmtpPassword);
        }

        using var mail = new MailMessage(_options.From!, message.To, message.Subject, message.HtmlBody)
        {
            IsBodyHtml = true
        };
        await client.SendMailAsync(mail, cancellationToken);
    }
}
