namespace GelyxLabs.Application.Abstractions.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string Locale);

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
