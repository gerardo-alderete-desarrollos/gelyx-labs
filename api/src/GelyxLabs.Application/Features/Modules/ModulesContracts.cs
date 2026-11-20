namespace GelyxLabs.Application.Features.Modules;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Entities;

public sealed record GetModulesQuery : IQuery<IReadOnlyList<ModuleDetailsDto>>;
public sealed record GetModuleByIdQuery(Guid Id) : IQuery<ModuleDetailsDto>;
public sealed record UpdateModuleCommand(
    Guid Id,
    IReadOnlyDictionary<string, ModuleTranslationInput> Translations,
    string? Icon,
    int SortOrder) : ICommand<ModuleDetailsDto>;
public sealed record SetModuleStatusCommand(Guid Id, bool IsActive) : ICommand;

public sealed record ModuleTranslationInput(string Name, string? Description);
public sealed record ModuleTranslationDto(string Name, string? Description);
public sealed record ModulePermissionDto(string Key, string Name);
public sealed record ModuleRoleDto(Guid Id, string Name);
public sealed record ModuleDetailsDto(
    Guid Id,
    string Key,
    IReadOnlyDictionary<string, ModuleTranslationDto> Translations,
    string? Icon,
    string? Route,
    int SortOrder,
    bool IsSystem,
    bool IsActive,
    IReadOnlyList<ModulePermissionDto> Permissions,
    IReadOnlyList<ModuleRoleDto> RolesWithAccess);

public interface IModulesRepository
{
    Task<IReadOnlyList<ModuleDetailsDto>> GetModulesAsync(CancellationToken cancellationToken);
    Task<ModuleDetailsDto?> GetModuleAsync(Guid id, CancellationToken cancellationToken);
    Task<AppModule?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public static class ModulesError
{
    public static readonly GelyxLabs.Domain.Errors.Error NotFound =
        new("module_not_found", "Module was not found.");
    public static readonly GelyxLabs.Domain.Errors.Error ProtectedModule =
        new("module_status_protected", "System modules, dashboard, and profile cannot be disabled.");
}
