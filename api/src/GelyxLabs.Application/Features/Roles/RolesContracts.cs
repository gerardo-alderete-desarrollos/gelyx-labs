namespace GelyxLabs.Application.Features.Roles;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleListItemDto>>;
public sealed record GetRoleByIdQuery(Guid Id) : IQuery<RoleDetailsDto>;
public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    Guid? CopyPermissionsFromRoleId) : ICommand<RoleDetailsDto>;
public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : ICommand<RoleDetailsDto>;
public sealed record DeleteRoleCommand(Guid Id, Guid? ReassignToRoleId) : ICommand;

public sealed record RoleListItemDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    int UserCount,
    int PermissionCount);

public sealed record RoleDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedAt,
    int UserCount,
    int PermissionCount,
    IReadOnlyList<string> Permissions);

public interface IRolesRepository
{
    Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<RoleDetailsDto?> GetRoleAsync(Guid id, CancellationToken cancellationToken);
    Task<AppRole?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, Guid? exceptRoleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetCopyablePermissionsAsync(Guid? sourceRoleId, CancellationToken cancellationToken);
    Task<IdentityResult> CreateAsync(AppRole role, IReadOnlyCollection<string> permissionKeys, CancellationToken cancellationToken);
    Task<IdentityResult> UpdateAsync(AppRole role, CancellationToken cancellationToken);
    Task<RoleDeletionResult> DeleteAsync(Guid roleId, Guid? reassignToRoleId, CancellationToken cancellationToken);
}

public sealed record RoleDeletionResult(bool Succeeded, string? ErrorCode = null, int UserCount = 0);

public static class RolesError
{
    public static readonly GelyxLabs.Domain.Errors.Error NotFound = new("role_not_found", "Role was not found.");
    public static readonly GelyxLabs.Domain.Errors.Error SystemRoleProtected = new("system_role_protected", "System roles cannot be changed or deleted.");
    public static readonly GelyxLabs.Domain.Errors.Error RoleNameInvalid = new("role_name_invalid", "Role name must contain 3 to 50 characters and cannot be Admin.");
    public static readonly GelyxLabs.Domain.Errors.Error RoleNameTaken = new("role_name_taken", "A role with this name already exists.");
    public static readonly GelyxLabs.Domain.Errors.Error CopySourceNotFound = new("copy_source_not_found", "The source role was not found.");
    public static readonly GelyxLabs.Domain.Errors.Error InvalidReassignment = new("invalid_reassignment", "The target role is not available for reassignment.");
    public static readonly GelyxLabs.Domain.Errors.Error RoleHasUsers = new("role_has_users", "The role has users assigned.");
    public static readonly GelyxLabs.Domain.Errors.Error OperationFailed = new("role_operation_failed", "The role operation could not be completed.");
}
