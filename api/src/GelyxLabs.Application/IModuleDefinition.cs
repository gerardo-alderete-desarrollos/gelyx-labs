using System.Collections.Generic;
using GelyxLabs.Domain.Entities;

namespace GelyxLabs.Application;

public interface IModuleDefinition
{
    string Key { get; }
    string Name { get; }
    string? Description { get; }
    IReadOnlyDictionary<string, ModuleTranslation> Translations { get; }
    string? Icon { get; }
    string? Route { get; }
    int SortOrder { get; }
    bool IsSystem { get; }
    IEnumerable<(string Permission, string Description)> GetPermissions();
}