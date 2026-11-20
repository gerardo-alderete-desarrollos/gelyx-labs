namespace GelyxLabs.Application.Features.Permissions;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed record GetPermissionModulesQuery : IQuery<IReadOnlyList<PermissionModuleDto>>;

public sealed record GetPermissionMatrixQuery : IQuery<PermissionMatrixDto>;

public sealed record GetRolePermissionsQuery(Guid RoleId) : IQuery<RolePermissionsDto>;

public sealed record UpdateRolePermissionsCommand(Guid RoleId, string[] Permissions) : ICommand;

public sealed record PermissionModuleDto(
    string Key,
    string Name,
    string? Description,
    string? Icon,
    string? Route,
    int SortOrder,
    bool IsSystem,
    IReadOnlyList<PermissionDto> Permissions);

public sealed record PermissionDto(string Key, string Name);

public sealed record PermissionMatrixDto(
    IReadOnlyList<RoleMatrixDto> Roles,
    IReadOnlyList<ModuleMatrixDto> Modules);

public sealed record RoleMatrixDto(
    Guid Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<RoleModuleMatrixDto> Modules);

public sealed record RoleModuleMatrixDto(
    string Key,
    IReadOnlyList<PermissionCellDto> Permissions);

public sealed record PermissionCellDto(string Key, string Name, bool Granted);

public sealed record ModuleMatrixDto(
    string Key,
    string Name,
    bool IsSystem,
    IReadOnlyList<PermissionDto> Permissions);

public sealed record RolePermissionsDto(Guid RoleId, string RoleName, IReadOnlyList<string> Permissions);
public sealed record RolePermissionChanges(IReadOnlyList<string> Granted, IReadOnlyList<string> Removed);

public sealed record PermissionsError(string Code, string Description)
{
    public static PermissionsError RoleNotFound(Guid roleId) =>
        new("role_not_found", $"Role '{roleId}' was not found.");

    public static PermissionsError AdminRoleProtected =>
        new("admin_role_protected", "The Admin role permissions cannot be edited.");

    public static PermissionsError SystemPermissionsRestricted =>
        new("system_permissions_restricted", "System module permissions can only be assigned to Admin.");
}

public interface IPermissionsRepository
{
    Task<IReadOnlyList<PermissionModuleDto>> GetModulesAsync(CancellationToken cancellationToken);
    Task<PermissionMatrixDto> GetMatrixAsync(CancellationToken cancellationToken);
    Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken);
    Task<Result<RolePermissionChanges>> UpdateRolePermissionsAsync(
        Guid roleId,
        string[] requestedKeys,
        CancellationToken cancellationToken);
}
