namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Abstractions.Modules;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ModuleCatalog : IModuleCatalog
{
    private readonly AppDbContext _context;

    public ModuleCatalog(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ActiveModule>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var modules = await _context.Modules
            .AsNoTracking()
            .Where(module => module.IsActive)
            .OrderBy(module => module.SortOrder)
            .ToListAsync(cancellationToken);

        return modules.Select(module => new ActiveModule(
            module.Key,
            module.Name,
            module.Translations.ToDictionary(
                translation => translation.Key,
                translation => translation.Value.Name,
                StringComparer.Ordinal),
            module.Icon,
            module.Route,
            module.SortOrder,
            module.IsSystem))
            .ToArray();
    }
}
