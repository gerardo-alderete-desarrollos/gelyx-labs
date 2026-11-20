namespace GelyxLabs.Api.Services.Email;

using Microsoft.Extensions.Options;

public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!new[] { "Logging", "Resend", "Smtp" }.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("Email:Provider must be Logging, Resend, or Smtp.");
        }

        if (string.Equals(options.Provider, "Resend", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.From))
            {
                return ValidateOptionsResult.Fail("Email:ApiKey and Email:From are required for the Resend provider.");
            }
        }

        if (string.Equals(options.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.From) || string.IsNullOrWhiteSpace(options.SmtpHost))
            {
                return ValidateOptionsResult.Fail("Email:From and Email:SmtpHost are required for the Smtp provider.");
            }

            if (options.SmtpPort is < 1 or > 65535)
            {
                return ValidateOptionsResult.Fail("Email:SmtpPort must be between 1 and 65535.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
