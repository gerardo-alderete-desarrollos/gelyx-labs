namespace GelyxLabs.Application.Features.Settings;

using System.Text.Json;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Settings;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Errors;

public sealed class GetSettingsHandler(IAppSettingsService settings) : IQueryHandler<GetSettingsQuery, IReadOnlyList<AppSettingValue>>
{
    public async Task<Result<IReadOnlyList<AppSettingValue>>> Handle(GetSettingsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await settings.GetAllAsync(cancellationToken));
}

public sealed class GetPublicSettingsHandler(IAppSettingsService settings) : IQueryHandler<GetPublicSettingsQuery, IReadOnlyList<AppSettingValue>>
{
    public async Task<Result<IReadOnlyList<AppSettingValue>>> Handle(GetPublicSettingsQuery query, CancellationToken cancellationToken)
    {
        var settingsValues = await settings.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AppSettingValue>>(
            settingsValues.Where(setting => SettingsCatalog.PublicKeys.Contains(setting.Key)).ToArray());
    }
}

public sealed class UpdateSettingHandler(IAppSettingsService settings, IAuditLogger auditLogger) : ICommandHandler<UpdateSettingCommand, AppSettingValue>
{
    public async Task<Result<AppSettingValue>> Handle(UpdateSettingCommand command, CancellationToken cancellationToken)
    {
        if (!SettingsCatalog.TryGetType(command.Key, out var expectedType))
        {
            return Result.Failure<AppSettingValue>(new Error("setting_not_found", "The setting was not found."));
        }

        try
        {
            using var document = JsonDocument.Parse(command.Value);
            if ((expectedType == SettingsCatalog.BooleanType && document.RootElement.ValueKind != JsonValueKind.True && document.RootElement.ValueKind != JsonValueKind.False) ||
                (expectedType == SettingsCatalog.StringType && document.RootElement.ValueKind != JsonValueKind.String))
            {
                return Result.Failure<AppSettingValue>(new Error("setting_type_invalid", "The setting value has an invalid type."));
            }

            if (command.Key == "app.defaultLocale" && document.RootElement.GetString() is not ("es" or "en"))
            {
                return Result.Failure<AppSettingValue>(new Error("setting_value_invalid", "The default locale is not supported."));
            }
        }
        catch (JsonException)
        {
            return Result.Failure<AppSettingValue>(new Error("setting_value_invalid", "The setting value must be valid JSON."));
        }

        var updated = await settings.UpdateAsync(command.Key, command.Value, expectedType, cancellationToken);
        await auditLogger.LogAsync(
            "setting.updated",
            "app_setting",
            command.Key,
            new Dictionary<string, object?> { ["key"] = command.Key, ["type"] = expectedType },
            cancellationToken);
        return Result.Success(updated);
    }
}
