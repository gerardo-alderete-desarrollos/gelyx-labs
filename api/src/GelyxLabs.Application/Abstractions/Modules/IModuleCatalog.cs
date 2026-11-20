namespace GelyxLabs.Application.Abstractions.Modules;

public interface IModuleCatalog
{
    Task<IReadOnlyList<ActiveModule>> GetActiveAsync(CancellationToken cancellationToken = default);
}

public sealed record ActiveModule(
    string Key,
    string Name,
    IReadOnlyDictionary<string, string> LocalizedNames,
    string? Icon,
    string? Route,
    int SortOrder,
    bool IsSystem);
