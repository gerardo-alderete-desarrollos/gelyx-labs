using Microsoft.AspNetCore.Identity;

namespace GelyxLabs.Domain.Entities;

public class AppRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}