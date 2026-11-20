namespace GelyxLabs.Application.Abstractions.Audit;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        IReadOnlyDictionary<string, object?> metadata,
        CancellationToken cancellationToken = default);
}
