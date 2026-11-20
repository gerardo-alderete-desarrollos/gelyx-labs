namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Settings;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public sealed class AppSettingsService : IAppSettingsService
{
    private const string CacheGenerationKey = "app-settings:generation";
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ICurrentUserService _currentUser;

    public AppSettingsService(AppDbContext context, IMemoryCache cache, ICurrentUserService currentUser)
    {
        _context = context;
        _cache = cache;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AppSettingValue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var generation = _cache.GetOrCreate(CacheGenerationKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return Guid.NewGuid().ToString("N");
        })!;

        return await _cache.GetOrCreateAsync($"app-settings:{generation}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return (IReadOnlyList<AppSettingValue>)await _context.AppSettings
                .AsNoTracking()
                .OrderBy(setting => setting.Key)
                .Select(setting => new AppSettingValue(
                    setting.Key,
                    setting.Value,
                    setting.Type,
                    setting.UpdatedAt,
                    setting.UpdatedBy))
                .ToArrayAsync(cancellationToken);
        }) ?? Array.Empty<AppSettingValue>();
    }

    public async Task<AppSettingValue?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).FirstOrDefault(setting => setting.Key == key);

    public async Task<bool> GetBooleanAsync(string key, bool fallback, CancellationToken cancellationToken = default)
    {
        var setting = await GetAsync(key, cancellationToken);
        return setting is not null && setting.Type == "boolean" && bool.TryParse(setting.Value, out var value)
            ? value
            : fallback;
    }

    public async Task<AppSettingValue> UpdateAsync(string key, string value, string type, CancellationToken cancellationToken = default)
    {
        var setting = await _context.AppSettings.SingleOrDefaultAsync(candidate => candidate.Key == key, cancellationToken);
        if (setting is null)
        {
            setting = new AppSetting { Key = key };
            _context.AppSettings.Add(setting);
        }

        setting.Value = value;
        setting.Type = type;
        setting.UpdatedAt = DateTimeOffset.UtcNow;
        setting.UpdatedBy = _currentUser.UserId;
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Set(CacheGenerationKey, Guid.NewGuid().ToString("N"), new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
        return new AppSettingValue(setting.Key, setting.Value, setting.Type, setting.UpdatedAt, setting.UpdatedBy);
    }
}
