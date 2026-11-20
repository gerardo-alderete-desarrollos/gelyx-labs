namespace GelyxLabs.Application.Abstractions.Token;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);
    string GenerateTwoFactorToken(Guid userId);
    Guid? ValidateTwoFactorToken(string token);
    (string PlainToken, string Hash) GenerateRefreshToken();
    string ComputeHash(string input);
}
