using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

namespace GelyxLabs.Application.Features.Audit;

public class AuditModule : IModuleDefinition
{
    public string Key => "audit";
    public string Name => "Auditoría";
    public string? Description => "Registro de auditoría del sistema";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Auditoría", Description = "Registro de auditoría del sistema" },
        ["en"] = new() { Name = "Audit", Description = "System audit log" }
    };
    public string? Icon => "activity";
    public string? Route => "/dashboard/audit";
    public int SortOrder => 40;
    public bool IsSystem => true;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("audit.access", "Acceso al módulo de auditoría");
        yield return ("audit.read", "Leer registros de auditoría");
    }
}