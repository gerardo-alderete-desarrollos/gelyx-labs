namespace GelyxLabs.Domain.Entities;

public class AppModule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable identifier used in permission keys, e.g. "users".</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Dictionary<string, ModuleTranslation> Translations { get; set; } = [];

    public string? Icon { get; set; }

    public string? Route { get; set; }

    public int SortOrder { get; set; }

    /// <summary>System modules are exclusive to the Admin role and cannot be renamed or removed.</summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Permission> Permissions { get; set; } = [];
}

public sealed class ModuleTranslation
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}