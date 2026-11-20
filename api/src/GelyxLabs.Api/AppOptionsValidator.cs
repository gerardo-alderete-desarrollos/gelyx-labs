namespace GelyxLabs.Api;

using Microsoft.Extensions.Options;

public class AppOptionsValidator : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        return ValidateOptionsResult.Success;
    }
}
