namespace GelyxLabs.Api;

using Microsoft.Extensions.Options;

public class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        return ValidateOptionsResult.Success;
    }
}
