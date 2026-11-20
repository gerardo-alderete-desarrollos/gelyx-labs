using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

namespace GelyxLabs.Application.Features.Profile;

public class ProfileModule : IModuleDefinition
{
    public string Key => "profile";
    public string Name => "Perfil";
    public string? Description => "Gestión del perfil de usuario";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Perfil", Description = "Gestión del perfil de usuario" },
        ["en"] = new() { Name = "Profile", Description = "User profile management" }
    };
    public string? Icon => "user";
    public string? Route => "/dashboard/profile";
    public int SortOrder => 5;
    public bool IsSystem => false;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("profile.access", "Acceso al perfil");
        yield return ("profile.update", "Actualizar perfil");
    }
}