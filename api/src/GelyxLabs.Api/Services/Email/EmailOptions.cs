namespace GelyxLabs.Api.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Logging";
    public string? ApiKey { get; set; }
    public string? From { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpUserName { get; set; }
    public string? SmtpPassword { get; set; }
}
