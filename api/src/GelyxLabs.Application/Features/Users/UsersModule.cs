using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

namespace GelyxLabs.Application.Features.Users;

public class UsersModule : IModuleDefinition
{
    public string Key => "users";
    public string Name => "Usuarios";
    public string? Description => "Gestión de usuarios del sistema";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Usuarios", Description = "Gestión de usuarios del sistema" },
        ["en"] = new() { Name = "Users", Description = "System user management" }
    };
    public string? Icon => "users";
    public string? Route => "/dashboard/users";
    public int SortOrder => 10;
    public bool IsSystem => true;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("users.access", "Acceso al módulo de usuarios");
        yield return ("users.read", "Leer usuarios");
        yield return ("users.create", "Crear usuarios");
        yield return ("users.update", "Actualizar usuarios");
        yield return ("users.delete", "Eliminar usuarios");
        yield return ("users.manage-status", "Activar o desactivar usuarios");
        yield return ("users.reset-password", "Restablecer contraseñas de usuarios");
        yield return ("users.assign-roles", "Asignar roles a usuarios");
    }
}
