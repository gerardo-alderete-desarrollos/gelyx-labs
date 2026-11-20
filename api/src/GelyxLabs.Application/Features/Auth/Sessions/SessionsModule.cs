namespace GelyxLabs.Application.Features.Auth.Sessions;

using GelyxLabs.Domain.Entities;

public sealed class SessionsModule : IModuleDefinition
{
    public string Key => "sessions";
    public string Name => "Sesiones";
    public string? Description => "Gestión de sesiones activas";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Sesiones", Description = "Gestión de sesiones activas" },
        ["en"] = new() { Name = "Sessions", Description = "Active session management" }
    };
    public string? Icon => "monitor-smartphone";
    public string? Route => "/dashboard/sessions";
    public int SortOrder => 6;
    public bool IsSystem => false;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("sessions.access", "Acceso a sesiones");
        yield return ("sessions.read", "Leer sesiones");
        yield return ("sessions.update", "Revocar sesiones");
    }
}
