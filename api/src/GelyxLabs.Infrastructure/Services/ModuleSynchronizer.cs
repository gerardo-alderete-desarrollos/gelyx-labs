using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GelyxLabs.Application;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;

namespace GelyxLabs.Infrastructure.Services;

public interface IModuleSynchronizer
{
    Task SynchronizeAsync(CancellationToken cancellationToken = default);
}

public class ModuleSynchronizer : IModuleSynchronizer
{
    private readonly AppDbContext _context;
    private readonly IEnumerable<IModuleDefinition> _moduleDefinitions;
    private readonly ILogger<ModuleSynchronizer> _logger;
    private readonly IPermissionService _permissionService;

    public ModuleSynchronizer(
        AppDbContext context,
        IEnumerable<IModuleDefinition> moduleDefinitions,
        ILogger<ModuleSynchronizer> logger,
        IPermissionService permissionService)
    {
        _context = context;
        _moduleDefinitions = moduleDefinitions;
        _logger = logger;
        _permissionService = permissionService;
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var definitions = _moduleDefinitions.ToList();
        var codeModuleKeys = definitions.Select(d => d.Key).ToHashSet();

        var existingModules = await _context.Modules
            .Include(m => m.Permissions)
            .ToListAsync(cancellationToken);

        var modulesByKey = existingModules.ToDictionary(m => m.Key, m => m);

        foreach (var definition in definitions)
        {
            if (modulesByKey.TryGetValue(definition.Key, out var existingModule))
            {
                FillMissingTranslations(existingModule, definition);
                await SynchronizePermissionsAsync(existingModule, definition, cancellationToken);
            }
            else
            {
                var newModule = CreateModule(definition);
                _context.Modules.Add(newModule);
                await SynchronizePermissionsAsync(newModule, definition, cancellationToken);
            }
        }

        foreach (var existingModule in existingModules)
        {
            if (!codeModuleKeys.Contains(existingModule.Key))
            {
                if (existingModule.IsActive)
                {
                    _logger.LogInformation("Module {ModuleKey} no longer exists in code, marking as inactive", existingModule.Key);
                    existingModule.IsActive = false;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AssignDefaultUserPermissionsAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _permissionService.InvalidateAll();
    }

    private static void FillMissingTranslations(AppModule module, IModuleDefinition definition)
    {
        if (module.Translations.Count == 0)
        {
            module.Translations = definition.Translations
                .ToDictionary(
                    translation => translation.Key,
                    translation => new ModuleTranslation
                    {
                        Name = translation.Value.Name,
                        Description = translation.Value.Description
                    },
                    StringComparer.Ordinal);
        }
    }

    private static AppModule CreateModule(IModuleDefinition definition)
    {
        return new AppModule
        {
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            Translations = definition.Translations.ToDictionary(
                translation => translation.Key,
                translation => new ModuleTranslation
                {
                    Name = translation.Value.Name,
                    Description = translation.Value.Description
                },
                StringComparer.Ordinal),
            Icon = definition.Icon,
            Route = definition.Route,
            SortOrder = definition.SortOrder,
            IsSystem = definition.IsSystem,
            IsActive = true
        };
    }

    private async Task SynchronizePermissionsAsync(
        AppModule module,
        IModuleDefinition definition,
        CancellationToken cancellationToken)
    {
        var codePermissions = definition.GetPermissions().ToList();
        var codePermissionKeys = codePermissions.Select(p => p.Permission).ToHashSet();

        var existingPermissions = module.Permissions.ToList();

        var newPermissions = new List<Permission>();

        foreach (var (permKey, permDesc) in codePermissions)
        {
            var existingPerm = existingPermissions.FirstOrDefault(p => p.Key == permKey);
            if (existingPerm is null)
            {
                var newPerm = new Permission
                {
                    Key = permKey,
                    Name = permDesc,
                    Module = module
                };
                module.Permissions.Add(newPerm);
                newPermissions.Add(newPerm);
            }
            else
            {
                existingPerm.Name = permDesc;
            }
        }

        foreach (var existingPerm in existingPermissions)
        {
            if (!codePermissionKeys.Contains(existingPerm.Key))
            {
                _logger.LogInformation("Permission {PermissionKey} no longer exists in code for module {ModuleKey}, keeping but not removing", existingPerm.Key, module.Key);
            }
        }

        if (newPermissions.Count > 0)
        {
            await AssignNewPermissionsToAdminAsync(newPermissions, cancellationToken);
        }
    }

    private async Task AssignNewPermissionsToAdminAsync(
        List<Permission> newPermissions,
        CancellationToken cancellationToken)
    {
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN", cancellationToken);

        if (adminRole is null)
        {
            _logger.LogWarning("Admin role not found, new permissions will not be auto-assigned");
            return;
        }

        foreach (var permission in newPermissions)
        {
            var alreadyAssigned = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id, cancellationToken);

            if (!alreadyAssigned)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
                _logger.LogInformation("Assigned new permission {PermissionKey} to Admin role", permission.Key);
            }
        }
    }

    private async Task AssignDefaultUserPermissionsAsync(CancellationToken cancellationToken)
    {
        var defaultPermissions = await _context.Permissions
            .Where(permission =>
                permission.Key == "dashboard.access" ||
                permission.Key == "profile.access" ||
                permission.Key == "profile.update")
            .ToArrayAsync(cancellationToken);
        var defaultPermissionIds = defaultPermissions.Select(permission => permission.Id).ToArray();
        var nonAdminSystemRoles = await _context.Roles
            .Where(role => role.IsSystem && role.NormalizedName != "ADMIN")
            .Select(role => role.Id)
            .ToArrayAsync(cancellationToken);

        foreach (var roleId in nonAdminSystemRoles)
        {
            var assignedPermissionIds = await _context.RolePermissions
                .Where(rolePermission =>
                    rolePermission.RoleId == roleId &&
                    defaultPermissionIds.Contains(rolePermission.PermissionId))
                .Select(rolePermission => rolePermission.PermissionId)
                .ToArrayAsync(cancellationToken);

            foreach (var permission in defaultPermissions.Where(
                         permission => !assignedPermissionIds.Contains(permission.Id)))
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id
                });
            }
        }

        var sessionPermissionIds = await _context.Permissions
            .Where(permission =>
                permission.Key == "sessions.access" ||
                permission.Key == "sessions.read" ||
                permission.Key == "sessions.update")
            .Select(permission => permission.Id)
            .ToArrayAsync(cancellationToken);
        var nonAdminRoleIds = await _context.Roles
            .Where(role => role.NormalizedName != "ADMIN")
            .Select(role => role.Id)
            .ToArrayAsync(cancellationToken);

        foreach (var roleId in nonAdminRoleIds)
        {
            var assignedSessionPermissionIds = await _context.RolePermissions
                .Where(rolePermission =>
                    rolePermission.RoleId == roleId &&
                    sessionPermissionIds.Contains(rolePermission.PermissionId))
                .Select(rolePermission => rolePermission.PermissionId)
                .ToArrayAsync(cancellationToken);

            foreach (var permissionId in sessionPermissionIds.Where(
                         permissionId => !assignedSessionPermissionIds.Contains(permissionId)))
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
            }
        }
    }
}