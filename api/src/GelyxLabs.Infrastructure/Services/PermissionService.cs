namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public sealed class PermissionService : IPermissionService
{
    private const string CacheGenerationKey = "effective-permissions:generation";
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var generation = _cache.GetOrCreate(CacheGenerationKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return Guid.NewGuid().ToString("N");
        })!;
        var userGenerationKey = $"effective-permissions:user-generation:{userId}";
        var userGeneration = _cache.GetOrCreate(userGenerationKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return Guid.NewGuid().ToString("N");
        })!;
        var permissionsCacheKey = $"effective-permissions:{generation}:{userGeneration}:{userId}";

        return await _cache.GetOrCreateAsync(permissionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(candidate => candidate.Id == userId)
                .Select(candidate => new { candidate.IsActive, candidate.IsDeleted })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null || !user.IsActive || user.IsDeleted)
            {
                return Array.Empty<string>();
            }

            var roles = await (
                from userRole in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == userId
                select new { role.Id, role.Name })
                .ToListAsync(cancellationToken);

            if (roles.Any(role => role.Name == "Admin"))
            {
                return await _context.Permissions
                    .AsNoTracking()
                    .OrderBy(permission => permission.Key)
                    .Select(permission => permission.Key)
                    .Distinct()
                    .ToArrayAsync(cancellationToken);
            }

            var roleIds = roles.Select(role => role.Id).ToArray();
            return await _context.RolePermissions
                .AsNoTracking()
                .Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
                .Select(rolePermission => rolePermission.Permission.Key)
                .Distinct()
                .OrderBy(permission => permission)
                .ToArrayAsync(cancellationToken);
        }) ?? Array.Empty<string>();
    }

    public void InvalidateUser(Guid userId)
    {
        _cache.Set(
            $"effective-permissions:user-generation:{userId}",
            Guid.NewGuid().ToString("N"),
            new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
    }

    public void InvalidateAll()
    {
        _cache.Set(
            CacheGenerationKey,
            Guid.NewGuid().ToString("N"),
            new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
    }
}
