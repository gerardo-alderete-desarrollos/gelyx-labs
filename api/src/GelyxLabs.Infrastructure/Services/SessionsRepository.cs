namespace GelyxLabs.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using GelyxLabs.Application.Features.Auth.Sessions;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class SessionsRepository : ISessionsRepository
{
    private readonly AppDbContext _context;

    public SessionsRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<SessionDto>> GetActiveAsync(
        Guid userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken)
    {
        var currentTokenHash = string.IsNullOrWhiteSpace(currentRefreshToken)
            ? null
            : ComputeHash(currentRefreshToken);
        var now = DateTimeOffset.UtcNow;

        return await _context.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null &&
                token.ExpiresAt > now)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => new SessionDto(
                token.Id,
                token.CreatedAt,
                token.ExpiresAt,
                token.CreatedByIp,
                token.Agent,
                currentTokenHash != null && token.TokenHash == currentTokenHash))
            .ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken?> RevokeAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.Id == id &&
                token.UserId == userId &&
                token.RevokedAt == null &&
                token.ExpiresAt > now,
                cancellationToken);
        if (token is null)
        {
            return null;
        }

        token.RevokedAt = now;
        token.RevokedReason = "session_revoked";
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<IReadOnlyList<RefreshToken>?> RevokeOthersAsync(
        Guid userId,
        string currentRefreshToken,
        CancellationToken cancellationToken)
    {
        var currentTokenHash = ComputeHash(currentRefreshToken);
        var now = DateTimeOffset.UtcNow;
        var currentSessionExists = await _context.RefreshTokens.AnyAsync(
            token =>
                token.UserId == userId &&
                token.TokenHash == currentTokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > now,
            cancellationToken);
        if (!currentSessionExists)
        {
            return null;
        }

        var otherSessions = await _context.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.TokenHash != currentTokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var session in otherSessions)
        {
            session.RevokedAt = now;
            session.RevokedReason = "other_sessions_revoked";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return otherSessions;
    }

    private static string ComputeHash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
