namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Features.Permissions;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class PermissionsRepository : IPermissionsRepository
{
    private readonly AppDbContext _context;

    public PermissionsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PermissionModuleDto>> GetModulesAsync(CancellationToken cancellationToken)
    {
        return await _context.Modules
            .AsNoTracking()
            .Where(module => module.IsActive)
            .OrderBy(module => module.SortOrder)
            .Select(module => new PermissionModuleDto(
                module.Key,
                module.Name,
                module.Description,
                module.Icon,
                module.Route,
                module.SortOrder,
                module.IsSystem,
                module.Permissions
                    .OrderBy(permission => permission.Key)
                    .Select(permission => new PermissionDto(permission.Key, permission.Name))
                    .ToArray()))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionMatrixDto> GetMatrixAsync(CancellationToken cancellationToken)
    {
        var modules = await _context.Modules
            .AsNoTracking()
            .Where(module => module.IsActive)
            .OrderBy(module => module.SortOrder)
            .Select(module => new ModuleMatrixDto(
                module.Key,
                module.Name,
                module.IsSystem,
                module.Permissions
                    .OrderBy(permission => permission.Key)
                    .Select(permission => new PermissionDto(permission.Key, permission.Name))
                    .ToArray()))
            .ToListAsync(cancellationToken);
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new { role.Id, role.Name, role.IsSystem })
            .ToListAsync(cancellationToken);
        var assignments = await _context.RolePermissions
            .AsNoTracking()
            .Select(assignment => new { assignment.RoleId, assignment.Permission.Key })
            .ToListAsync(cancellationToken);
        var assignmentsByRole = assignments
            .GroupBy(assignment => assignment.RoleId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(assignment => assignment.Key).ToHashSet(StringComparer.Ordinal));
        var matrixRoles = roles.Select(role =>
        {
            assignmentsByRole.TryGetValue(role.Id, out var assignedKeys);
            assignedKeys ??= new HashSet<string>(StringComparer.Ordinal);
            return new RoleMatrixDto(
                role.Id,
                role.Name!,
                role.IsSystem,
                modules.Select(module => new RoleModuleMatrixDto(
                    module.Key,
                    module.Permissions.Select(permission => new PermissionCellDto(
                        permission.Key,
                        permission.Name,
                        role.Name == "Admin" || assignedKeys.Contains(permission.Key)))
                        .ToArray()))
                    .ToArray());
        }).ToArray();

        return new PermissionMatrixDto(
            matrixRoles,
            modules.Select(module => new ModuleMatrixDto(
                module.Key,
                module.Name,
                module.IsSystem,
                module.Permissions))
                .ToArray());
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(candidate => candidate.Id == roleId)
            .Select(candidate => new { candidate.Id, candidate.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (role is null)
        {
            return null;
        }

        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(assignment => assignment.RoleId == roleId)
            .Select(assignment => assignment.Permission.Key)
            .OrderBy(key => key)
            .ToArrayAsync(cancellationToken);
        return new RolePermissionsDto(role.Id, role.Name!, permissions);
    }

    public async Task<Result<RolePermissionChanges>> UpdateRolePermissionsAsync(
        Guid roleId,
        string[] permissionKeys,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .SingleOrDefaultAsync(candidate => candidate.Id == roleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RolePermissionChanges>(new GelyxLabs.Domain.Errors.Error("role_not_found", $"Role '{roleId}' was not found."));
        }

        if (role.Name == "Admin")
        {
            return Result.Failure<RolePermissionChanges>(new GelyxLabs.Domain.Errors.Error(
                "admin_role_protected",
                "The Admin role permissions cannot be edited."));
        }

        var requestedKeys = permissionKeys.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var permissions = await _context.Permissions
            .Include(permission => permission.Module)
            .ToListAsync(cancellationToken);
        var permissionsByKey = permissions.ToDictionary(permission => permission.Key, StringComparer.Ordinal);
        var unknownKey = requestedKeys.FirstOrDefault(key => !permissionsByKey.ContainsKey(key));
        if (unknownKey is not null)
        {
            return Result.Failure<RolePermissionChanges>(new GelyxLabs.Domain.Errors.Error(
                "permission_not_found",
                $"Permission '{unknownKey}' was not found."));
        }

        if (requestedKeys.Any(key => permissionsByKey[key].Module.IsSystem))
        {
            return Result.Failure<RolePermissionChanges>(new GelyxLabs.Domain.Errors.Error(
                "system_permissions_restricted",
                "System module permissions can only be assigned to Admin."));
        }

        foreach (var permissionKey in requestedKeys.ToArray())
        {
            var moduleKey = permissionsByKey[permissionKey].Module.Key;
            var accessKey = $"{moduleKey}.access";
            if (permissionsByKey.ContainsKey(accessKey))
            {
                requestedKeys.Add(accessKey);
            }
        }

        foreach (var module in permissions.Select(permission => permission.Module).DistinctBy(module => module.Id))
        {
            if (!requestedKeys.Contains($"{module.Key}.access"))
            {
                requestedKeys.RemoveWhere(key =>
                    permissionsByKey.TryGetValue(key, out var permission) &&
                    permission.ModuleId == module.Id);
            }
        }

        var existing = await _context.RolePermissions
            .Where(assignment => assignment.RoleId == role.Id)
            .Include(assignment => assignment.Permission)
            .ToListAsync(cancellationToken);
        var existingKeys = existing.Select(assignment => assignment.Permission.Key).ToHashSet(StringComparer.Ordinal);
        var granted = requestedKeys.Except(existingKeys, StringComparer.Ordinal).OrderBy(key => key).ToArray();
        var removed = existingKeys.Except(requestedKeys, StringComparer.Ordinal).OrderBy(key => key).ToArray();
        _context.RolePermissions.RemoveRange(existing);
        foreach (var permissionKey in requestedKeys)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionsByKey[permissionKey].Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(new RolePermissionChanges(granted, removed));
    }
}
