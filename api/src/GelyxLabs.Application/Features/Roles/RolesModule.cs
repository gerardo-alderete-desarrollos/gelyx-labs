using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

namespace GelyxLabs.Application.Features.Roles;

public class RolesModule : IModuleDefinition
{
    public string Key => "roles";
    public string Name => "Roles";
    public string? Description => "Gestión de roles y permisos";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Roles", Description = "Gestión de roles y permisos" },
        ["en"] = new() { Name = "Roles", Description = "Role and permission management" }
    };
    public string? Icon => "shield";
    public string? Route => "/dashboard/roles";
    public int SortOrder => 20;
    public bool IsSystem => true;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("roles.access", "Acceso al módulo de roles");
        yield return ("roles.read", "Leer roles");
        yield return ("roles.create", "Crear roles");
        yield return ("roles.update", "Actualizar roles");
        yield return ("roles.delete", "Eliminar roles");
    }
}