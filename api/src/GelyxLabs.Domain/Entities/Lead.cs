namespace GelyxLabs.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public string? Service { get; set; }

    public string? Budget { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "new";

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ContactedAt { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }

    public string? ResponseChannel { get; set; }
}