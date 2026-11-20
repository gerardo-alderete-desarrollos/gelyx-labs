namespace GelyxLabs.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Metadata { get; set; } = "{}";
    public string? Ip { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
