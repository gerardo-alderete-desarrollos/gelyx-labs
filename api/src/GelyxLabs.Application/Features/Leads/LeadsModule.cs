namespace GelyxLabs.Application.Features.Leads;

using GelyxLabs.Application;
using GelyxLabs.Domain.Entities;
using System.Collections.Generic;

public class LeadsModule : IModuleDefinition
{
    public string Key => "leads";
    public string Name => "Leads";
    public string? Description => "Captación de clientes y cotizaciones";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
    {
        ["es"] = new() { Name = "Leads", Description = "Captación de clientes y cotizaciones" },
        ["en"] = new() { Name = "Leads", Description = "Client capture and quotes" }
    };
    public string? Icon => "inbox";
    public string? Route => "/dashboard/leads";
    public int SortOrder => 55;
    public bool IsSystem => false;

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        yield return ("leads.access", "Acceso al módulo de leads");
        yield return ("leads.read", "Leer leads");
        yield return ("leads.create", "Crear leads");
        yield return ("leads.update", "Actualizar leads");
        yield return ("leads.delete", "Eliminar leads");
        yield return ("leads.respond", "Responder a leads");
    }
}