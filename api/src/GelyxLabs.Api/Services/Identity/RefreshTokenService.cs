using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Domain;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http;

namespace GelyxLabs.Api.Services.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RefreshTokenResult> CreateAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var refreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = ComputeHash(refreshToken),
            CreatedByIp = ipAddress ?? GetIpAddress(),
            Agent = GetAgent(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            RevokedAt = null
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(true, refreshToken, 7 * 24 * 60);
    }

    public async Task<ValidateRefreshTokenResult> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeHash(refreshToken);
        var token = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return new ValidateRefreshTokenResult(false, Guid.Empty, "token_invalid");
        }

        if (token.ReplacedByTokenHash is not null)
        {
            await RevokeAllAsync(token.UserId, "token_reused", cancellationToken);
            return new ValidateRefreshTokenResult(false, Guid.Empty, "token_reused");
        }

        if (token.RevokedAt is not null || token.IsExpired || token.User.IsDeleted || !token.User.IsActive)
        {
            return new ValidateRefreshTokenResult(false, Guid.Empty, "token_invalid");
        }

        return new ValidateRefreshTokenResult(true, token.UserId);
    }

    public async Task<RotateRefreshTokenResult> RotateAsync(string currentRefreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var currentTokenHash = ComputeHash(currentRefreshToken);
        var token = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == currentTokenHash, cancellationToken);

        if (token is null)
        {
            return new RotateRefreshTokenResult(false, Guid.Empty, ErrorCode: "token_invalid");
        }

        if (token.ReplacedByTokenHash is not null)
        {
            await RevokeAllAsync(token.UserId, "token_reused", cancellationToken);
            return new RotateRefreshTokenResult(false, token.UserId, ErrorCode: "token_reused");
        }

        if (token.RevokedAt is not null || token.IsExpired || token.User.IsDeleted || !token.User.IsActive)
        {
            return new RotateRefreshTokenResult(false, token.UserId, ErrorCode: "token_invalid");
        }

        var now = DateTimeOffset.UtcNow;
        var replacementRefreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var replacement = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = ComputeHash(replacementRefreshToken),
            CreatedByIp = ipAddress ?? GetIpAddress(),
            Agent = GetAgent(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        };

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync<RotateRefreshTokenResult>(async strategyCancellationToken =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(strategyCancellationToken);
            var updatedRows = await _context.RefreshTokens
                .IgnoreQueryFilters()
                .Where(rt =>
                    rt.TokenHash == currentTokenHash &&
                    rt.RevokedAt == null &&
                    rt.ReplacedByTokenHash == null &&
                    rt.ExpiresAt > now &&
                    !rt.User.IsDeleted &&
                    rt.User.IsActive)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(rt => rt.RevokedAt, now)
                    .SetProperty(rt => rt.RevokedReason, "token_rotated")
                    .SetProperty(rt => rt.ReplacedByTokenHash, replacement.TokenHash),
                    strategyCancellationToken);

            if (updatedRows == 0)
            {
                var currentToken = await _context.RefreshTokens
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rt => rt.TokenHash == currentTokenHash, strategyCancellationToken);

                if (currentToken?.ReplacedByTokenHash is not null)
                {
                    await RevokeAllAsync(currentToken.UserId, "token_reused", strategyCancellationToken);
                    await transaction.CommitAsync(strategyCancellationToken);
                    return new RotateRefreshTokenResult(false, currentToken.UserId, ErrorCode: "token_reused");
                }

                await transaction.RollbackAsync(strategyCancellationToken);
                return new RotateRefreshTokenResult(false, token.UserId, ErrorCode: "token_invalid");
            }

            _context.RefreshTokens.Add(replacement);
            await _context.SaveChangesAsync(strategyCancellationToken);
            await transaction.CommitAsync(strategyCancellationToken);

            return new RotateRefreshTokenResult(
                true,
                token.UserId,
                RefreshToken: replacementRefreshToken);
        }, cancellationToken);
    }

    public async Task<RevokeResult> RevokeAsync(string refreshToken, string reason, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeHash(refreshToken);
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return new RevokeResult(false, "token_not_found");
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        return new RevokeResult(true);
    }

    public async Task<RevokeResult> RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow)
                .SetProperty(rt => rt.RevokedReason, reason),
            cancellationToken);

        return new RevokeResult(true);
    }

    public async Task<RevokeResult> RevokeAllExceptAsync(Guid userId, string exceptRefreshToken, string reason, CancellationToken cancellationToken = default)
    {
        var exceptTokenHash = ComputeHash(exceptRefreshToken);
        await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.TokenHash != exceptTokenHash)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow)
                .SetProperty(rt => rt.RevokedReason, reason),
            cancellationToken);

        return new RevokeResult(true);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private string? GetIpAddress() =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? GetAgent()
    {
        var agent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(agent)
            ? null
            : agent[..Math.Min(agent.Length, 512)];
    }
}
