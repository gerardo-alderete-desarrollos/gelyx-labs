namespace GelyxLabs.Application.Abstractions.Settings;

public interface IAppSettingsService
{
    Task<IReadOnlyList<AppSettingValue>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppSettingValue?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> GetBooleanAsync(string key, bool fallback, CancellationToken cancellationToken = default);
    Task<AppSettingValue> UpdateAsync(string key, string value, string type, CancellationToken cancellationToken = default);
}

public sealed record AppSettingValue(string Key, string Value, string Type, DateTimeOffset UpdatedAt, Guid? UpdatedBy);
