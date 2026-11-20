namespace GelyxLabs.Application.Features.Projects;

using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

public class ProjectsModule : IModuleDefinition
{
    public string Key => "projects";
    public string Name => "Proyectos";
    public string? Description => "Gestión del portafolio de proyectos";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Proyectos", Description = "Gestión del portafolio de proyectos" },
        ["en"] = new() { Name = "Projects", Description = "Project portfolio management" }
    };
    public string? Icon => "briefcase";
    public string? Route => "/dashboard/projects";
    public int SortOrder => 60;
    public bool IsSystem => false;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("projects.access", "Acceso al módulo de proyectos");
        yield return ("projects.read", "Leer proyectos");
        yield return ("projects.create", "Crear proyectos");
        yield return ("projects.update", "Actualizar proyectos");
        yield return ("projects.delete", "Eliminar proyectos");
    }
}