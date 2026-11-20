namespace GelyxLabs.Application.Features.Auth.Sessions;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Entities;

public sealed record GetSessionsQuery(string? CurrentRefreshToken) : IQuery<IReadOnlyList<SessionDto>>;

public sealed record RevokeSessionCommand(Guid Id) : ICommand;

public sealed record RevokeOtherSessionsCommand(string CurrentRefreshToken) : ICommand<RevokeOtherSessionsResponse>;

public sealed record SessionDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string? CreatedByIp,
    string? Agent,
    bool Current);

public sealed record RevokeOtherSessionsResponse(int RevokedCount);

public interface ISessionsRepository
{
    Task<IReadOnlyList<SessionDto>> GetActiveAsync(
        Guid userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken);

    Task<RefreshToken?> RevokeAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>?> RevokeOthersAsync(
        Guid userId,
        string currentRefreshToken,
        CancellationToken cancellationToken);
}
