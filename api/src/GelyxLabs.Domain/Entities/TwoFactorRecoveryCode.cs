namespace GelyxLabs.Domain.Entities;

public sealed class TwoFactorRecoveryCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public AppUser User { get; set; } = null!;
}
