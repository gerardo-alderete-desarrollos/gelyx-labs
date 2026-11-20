using Microsoft.AspNetCore.Identity;

namespace GelyxLabs.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }

    public string Locale { get; set; } = "es";

    public string Theme { get; set; } = "system";

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public uint RowVersion { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}