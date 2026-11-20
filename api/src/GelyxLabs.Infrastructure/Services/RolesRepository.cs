using GelyxLabs.Application.Features.Roles;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GelyxLabs.Infrastructure.Services;

public sealed class RolesRepository : IRolesRepository
{
    private readonly AppDbContext _context;
    private readonly RoleManager<AppRole> _roleManager;

    public RolesRepository(AppDbContext context, RoleManager<AppRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                role.Name,
                role.Description,
                role.IsSystem,
                UserCount = _context.UserRoles.Count(userRole => userRole.RoleId == role.Id),
                PermissionCount = _context.RolePermissions.Count(rolePermission => rolePermission.RoleId == role.Id)
            })
            .ToArrayAsync(cancellationToken);
        return roles.Select(role => new RoleListItemDto(
            role.Id,
            role.Name!,
            role.Description,
            role.IsSystem,
            role.UserCount,
            role.PermissionCount)).ToArray();
    }

    public async Task<RoleDetailsDto?> GetRoleAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
                candidate.Description,
                candidate.IsSystem,
                candidate.CreatedAt,
                UserCount = _context.UserRoles.Count(userRole => userRole.RoleId == candidate.Id),
                PermissionCount = _context.RolePermissions.Count(rolePermission => rolePermission.RoleId == candidate.Id)
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (role is null)
        {
            return null;
        }

        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == id)
            .Select(rolePermission => rolePermission.Permission.Key)
            .OrderBy(key => key)
            .ToArrayAsync(cancellationToken);
        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            permissions = await _context.Permissions.AsNoTracking()
                .OrderBy(permission => permission.Key)
                .Select(permission => permission.Key)
                .ToArrayAsync(cancellationToken);
        }

        return new RoleDetailsDto(
            role.Id,
            role.Name!,
            role.Description,
            role.IsSystem,
            role.CreatedAt,
            role.UserCount,
            role.PermissionCount,
            permissions);
    }

    public Task<AppRole?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Roles.SingleOrDefaultAsync(role => role.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptRoleId, CancellationToken cancellationToken)
    {
        var normalizedName = _roleManager.NormalizeKey(name);
        var roles = _context.Roles.Where(role => role.NormalizedName == normalizedName);
        if (exceptRoleId is { } id)
        {
            roles = roles.Where(role => role.Id != id);
        }
        return roles.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCopyablePermissionsAsync(
        Guid? sourceRoleId,
        CancellationToken cancellationToken)
    {
        if (sourceRoleId is { } id)
        {
            return await _context.RolePermissions
                .AsNoTracking()
                .Where(rolePermission =>
                    rolePermission.RoleId == id &&
                    !rolePermission.Permission.Module.IsSystem)
                .Select(rolePermission => rolePermission.Permission.Key)
                .Distinct()
                .OrderBy(key => key)
                .ToArrayAsync(cancellationToken);
        }

        return await _context.Permissions
            .AsNoTracking()
            .Where(permission =>
                !permission.Module.IsSystem &&
                (permission.Key == "dashboard.access" ||
                 permission.Key == "profile.access" ||
                 permission.Key == "profile.update"))
            .Select(permission => permission.Key)
            .OrderBy(key => key)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IdentityResult> CreateAsync(
        AppRole role,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var createResult = await _roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return createResult;
            }

            var permissions = await _context.Permissions
                .Where(permission =>
                    permissionKeys.Contains(permission.Key) &&
                    !permission.Module.IsSystem)
                .ToArrayAsync(cancellationToken);
            foreach (var permission in permissions)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return IdentityResult.Success;
        });
    }

    public Task<IdentityResult> UpdateAsync(AppRole role, CancellationToken cancellationToken) =>
        _roleManager.UpdateAsync(role);

    public async Task<RoleDeletionResult> DeleteAsync(
        Guid roleId,
        Guid? reassignToRoleId,
        CancellationToken cancellationToken)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var role = await _context.Roles.SingleOrDefaultAsync(candidate => candidate.Id == roleId, cancellationToken);
            if (role is null)
            {
                return new RoleDeletionResult(false, "role_not_found");
            }

            var userLinks = await _context.UserRoles
                .Where(userRole => userRole.RoleId == roleId)
                .ToArrayAsync(cancellationToken);
            if (userLinks.Length > 0 && reassignToRoleId is null)
            {
                return new RoleDeletionResult(false, "role_has_users", userLinks.Length);
            }
            if (reassignToRoleId is { } targetId)
            {
                var target = await _context.Roles.SingleOrDefaultAsync(candidate => candidate.Id == targetId, cancellationToken);
                if (target is null || target.Id == roleId)
                {
                    return new RoleDeletionResult(false, "invalid_reassignment", userLinks.Length);
                }

                var userIds = userLinks.Select(link => link.UserId).Distinct().ToArray();
                foreach (var userId in userIds)
                {
                    var alreadyAssigned = await _context.UserRoles.AnyAsync(
                        userRole => userRole.UserId == userId && userRole.RoleId == target.Id,
                        cancellationToken);
                    if (!alreadyAssigned)
                    {
                        _context.UserRoles.Add(new IdentityUserRole<Guid>
                        {
                            UserId = userId,
                            RoleId = target.Id
                        });
                    }
                }
                _context.UserRoles.RemoveRange(userLinks);
                await _context.RefreshTokens
                    .IgnoreQueryFilters()
                    .Where(token => userIds.Contains(token.UserId) && token.RevokedAt == null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(token => token.RevokedAt, DateTimeOffset.UtcNow)
                        .SetProperty(token => token.RevokedReason, "role_reassigned"),
                        cancellationToken);
            }

            var permissions = await _context.RolePermissions
                .Where(rolePermission => rolePermission.RoleId == roleId)
                .ToArrayAsync(cancellationToken);
            _context.RolePermissions.RemoveRange(permissions);
            var deleteResult = await _roleManager.DeleteAsync(role);
            if (!deleteResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RoleDeletionResult(false, "role_operation_failed", userLinks.Length);
            }
            await transaction.CommitAsync(cancellationToken);
            return new RoleDeletionResult(true, UserCount: userLinks.Length);
        });
    }
}
