namespace GelyxLabs.Application.Features.Modules;

using GelyxLabs.Domain.Entities;

public sealed class ModulesModule : IModuleDefinition
{
    public string Key => "modules";
    public string Name => "Módulos";
    public string? Description => "Configuración visual de módulos";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Módulos", Description = "Configuración visual de módulos" },
        ["en"] = new() { Name = "Modules", Description = "Module visual configuration" }
    };
    public string? Icon => "boxes";
    public string? Route => "/dashboard/modules";
    public int SortOrder => 50;
    public bool IsSystem => true;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("modules.access", "Acceso al módulo de módulos");
        yield return ("modules.read", "Leer módulos");
        yield return ("modules.update", "Actualizar módulos");
    }
}
