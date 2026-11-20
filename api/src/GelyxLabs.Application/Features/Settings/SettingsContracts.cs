namespace GelyxLabs.Application.Features.Settings;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Settings;

public sealed record GetSettingsQuery : IQuery<IReadOnlyList<AppSettingValue>>;
public sealed record GetPublicSettingsQuery : IQuery<IReadOnlyList<AppSettingValue>>;
public sealed record UpdateSettingCommand(string Key, string Value) : ICommand<AppSettingValue>;

public static class SettingsCatalog
{
    public const string StringType = "string";
    public const string BooleanType = "boolean";

    private static readonly IReadOnlyDictionary<string, string> KnownSettings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["app.name"] = StringType,
        ["auth.allowSelfRegistration"] = BooleanType,
        ["app.defaultLocale"] = StringType
    };

    public static readonly IReadOnlySet<string> PublicKeys = new HashSet<string>(KnownSettings.Keys, StringComparer.Ordinal);

    public static bool TryGetType(string key, out string type)
    {
        if (KnownSettings.TryGetValue(key, out type!)) return true;
        if (key.StartsWith("flags.", StringComparison.Ordinal) && key.Length > "flags.".Length &&
            key["flags.".Length..].All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
        {
            type = BooleanType;
            return true;
        }

        type = string.Empty;
        return false;
    }
}
