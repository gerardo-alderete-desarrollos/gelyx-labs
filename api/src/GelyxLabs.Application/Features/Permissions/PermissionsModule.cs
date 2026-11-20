using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

namespace GelyxLabs.Application.Features.Permissions;

public class PermissionsModule : IModuleDefinition
{
    public string Key => "permissions";
    public string Name => "Permisos";
    public string? Description => "Matriz de permisos por rol";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Permisos", Description = "Matriz de permisos por rol" },
        ["en"] = new() { Name = "Permissions", Description = "Role permission matrix" }
    };
    public string? Icon => "key";
    public string? Route => "/dashboard/permissions";
    public int SortOrder => 30;
    public bool IsSystem => true;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("permissions.access", "Acceso al módulo de permisos");
        yield return ("permissions.read", "Leer permisos");
        yield return ("permissions.manage", "Gestionar asignación de permisos");
    }
}