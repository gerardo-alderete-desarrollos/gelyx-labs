namespace GelyxLabs.Application.Features.Settings;

using GelyxLabs.Domain.Entities;

public sealed class SettingsModule : IModuleDefinition
{
    public string Key => "settings";
    public string Name => "Configuración";
    public string? Description => "Configuración general del sistema";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Configuración", Description = "Configuración general del sistema" },
        ["en"] = new() { Name = "Settings", Description = "General system settings" }
    };
    public string? Icon => "settings";
    public string? Route => "/dashboard/settings";
    public int SortOrder => 50;
    public bool IsSystem => true;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("settings.access", "Acceso a configuración");
        yield return ("settings.read", "Leer configuración");
        yield return ("settings.update", "Actualizar configuración");
    }
}
