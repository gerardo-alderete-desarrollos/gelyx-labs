namespace GelyxLabs.Application.Abstractions.Token;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> CreateAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ValidateRefreshTokenResult> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<RotateRefreshTokenResult> RotateAsync(string currentRefreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<RevokeResult> RevokeAsync(string refreshToken, string reason, CancellationToken cancellationToken = default);
    Task<RevokeResult> RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
    Task<RevokeResult> RevokeAllExceptAsync(Guid userId, string exceptRefreshToken, string reason, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenResult(
    bool Success,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    string? ErrorCode = null);

public sealed record ValidateRefreshTokenResult(
    bool Success,
    Guid UserId,
    string? ErrorCode = null);

public sealed record RotateRefreshTokenResult(
    bool Success,
    Guid UserId,
    string? RefreshToken = null,
    string? ErrorCode = null);

public sealed record RevokeResult(
    bool Success,
    string? ErrorCode = null);
