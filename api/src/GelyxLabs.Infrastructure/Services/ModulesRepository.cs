namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Features.Modules;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ModulesRepository : IModulesRepository
{
    private readonly AppDbContext _context;

    public ModulesRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ModuleDetailsDto>> GetModulesAsync(CancellationToken cancellationToken)
    {
        var modules = await _context.Modules
            .AsNoTracking()
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => module.Key)
            .ToListAsync(cancellationToken);
        return await MapAsync(modules, cancellationToken);
    }

    public async Task<ModuleDetailsDto?> GetModuleAsync(Guid id, CancellationToken cancellationToken)
    {
        var module = await _context.Modules
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (module is null)
        {
            return null;
        }
        return (await MapAsync([module], cancellationToken)).Single();
    }

    public Task<AppModule?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Modules.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    private async Task<IReadOnlyList<ModuleDetailsDto>> MapAsync(
        IReadOnlyList<AppModule> modules,
        CancellationToken cancellationToken)
    {
        var moduleIds = modules.Select(module => module.Id).ToArray();
        var permissions = await _context.Permissions
            .AsNoTracking()
            .Where(permission => moduleIds.Contains(permission.ModuleId))
            .Select(permission => new
            {
                permission.ModuleId,
                permission.Key,
                permission.Name
            })
            .ToListAsync(cancellationToken);
        var roleAccess = await _context.RolePermissions
            .AsNoTracking()
            .Where(assignment => moduleIds.Contains(assignment.Permission.ModuleId) &&
                                 assignment.Permission.Key.EndsWith(".access"))
            .Select(assignment => new
            {
                assignment.Permission.ModuleId,
                assignment.Role.Id,
                Name = assignment.Role.Name!
            })
            .ToListAsync(cancellationToken);
        var rolesByModule = roleAccess
            .GroupBy(access => access.ModuleId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ModuleRoleDto>)group
                    .OrderBy(access => access.Name)
                    .Select(access => new ModuleRoleDto(access.Id, access.Name))
                    .ToArray());
        var permissionsByModule = permissions
            .GroupBy(permission => permission.ModuleId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ModulePermissionDto>)group
                    .OrderBy(permission => permission.Key)
                    .Select(permission => new ModulePermissionDto(permission.Key, permission.Name))
                    .ToArray());

        return modules.Select(module =>
        {
            rolesByModule.TryGetValue(module.Id, out var roles);
            permissionsByModule.TryGetValue(module.Id, out var modulePermissions);
            return new ModuleDetailsDto(
                module.Id,
                module.Key,
                module.Translations.ToDictionary(
                    translation => translation.Key,
                    translation => new ModuleTranslationDto(translation.Value.Name, translation.Value.Description),
                    StringComparer.Ordinal),
                module.Icon,
                module.Route,
                module.SortOrder,
                module.IsSystem,
                module.IsActive,
                modulePermissions ?? [],
                roles ?? []);
        }).ToArray();
    }
}
