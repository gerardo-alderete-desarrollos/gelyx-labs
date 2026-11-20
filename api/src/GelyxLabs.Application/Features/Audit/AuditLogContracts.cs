namespace GelyxLabs.Application.Features.Audit;

using GelyxLabs.Application.Abstractions.Cqrs;

public sealed record GetAuditLogsQuery(
    int Page,
    int PageSize,
    string? EntityType,
    string? Action,
    Guid? ActorUserId) : IQuery<AuditLogsPageDto>;

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string? EntityId,
    System.Text.Json.JsonElement Metadata,
    string? Ip,
    DateTimeOffset CreatedAt);

public sealed record AuditLogsPageDto(
    IReadOnlyList<AuditLogDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public interface IAuditLogRepository
{
    Task<AuditLogsPageDto> GetAsync(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken);
    IAsyncEnumerable<AuditLogDto> StreamAsync(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken);
}
