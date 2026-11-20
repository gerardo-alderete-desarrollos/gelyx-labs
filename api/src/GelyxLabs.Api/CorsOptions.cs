namespace GelyxLabs.Api;

using Microsoft.Extensions.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    public static CorsOptions FromConfiguration(IConfigurationSection section)
    {
        var scalarOrigins = section[nameof(AllowedOrigins)];
        var origins = !string.IsNullOrWhiteSpace(scalarOrigins)
            ? scalarOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : section.GetSection(nameof(AllowedOrigins))
                .GetChildren()
                .Select(origin => origin.Value)
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin!)
                .ToArray();
        return new CorsOptions { AllowedOrigins = origins };
    }
}

public class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        return ValidateOptionsResult.Success;
    }
}
