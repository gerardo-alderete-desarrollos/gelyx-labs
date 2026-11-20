namespace GelyxLabs.Domain.Entities;

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Permission key following the "&lt;module&gt;.&lt;action&gt;" convention, e.g. "users.create".</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid ModuleId { get; set; }

    public AppModule Module { get; set; } = null!;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}