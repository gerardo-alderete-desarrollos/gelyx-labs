using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

namespace GelyxLabs.Application.Features.Dashboard;

public class DashboardModule : IModuleDefinition
{
    public string Key => "dashboard";
    public string Name => "Panel";
    public string? Description => "Panel principal del sistema";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Panel", Description = "Panel principal del sistema" },
        ["en"] = new() { Name = "Dashboard", Description = "Main system dashboard" }
    };
    public string? Icon => "layout-dashboard";
    public string? Route => "/dashboard";
    public int SortOrder => 1;
    public bool IsSystem => false;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("dashboard.access", "Acceso al panel principal");
    }
}