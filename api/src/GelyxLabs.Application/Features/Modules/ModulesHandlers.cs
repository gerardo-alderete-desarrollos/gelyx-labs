namespace GelyxLabs.Application.Features.Modules;

using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;

public sealed class GetModulesHandler : IQueryHandler<GetModulesQuery, IReadOnlyList<ModuleDetailsDto>>
{
    private readonly IModulesRepository _repository;

    public GetModulesHandler(IModulesRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ModuleDetailsDto>>> Handle(
        GetModulesQuery query,
        CancellationToken cancellationToken) =>
        Result.Success(await _repository.GetModulesAsync(cancellationToken));
}

public sealed class GetModuleByIdHandler : IQueryHandler<GetModuleByIdQuery, ModuleDetailsDto>
{
    private readonly IModulesRepository _repository;

    public GetModuleByIdHandler(IModulesRepository repository) => _repository = repository;

    public async Task<Result<ModuleDetailsDto>> Handle(
        GetModuleByIdQuery query,
        CancellationToken cancellationToken)
    {
        var module = await _repository.GetModuleAsync(query.Id, cancellationToken);
        return module is null
            ? Result.Failure<ModuleDetailsDto>(ModulesError.NotFound)
            : Result.Success(module);
    }
}

public sealed class UpdateModuleHandler : ICommandHandler<UpdateModuleCommand, ModuleDetailsDto>
{
    private readonly IModulesRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public UpdateModuleHandler(
        IModulesRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<ModuleDetailsDto>> Handle(
        UpdateModuleCommand command,
        CancellationToken cancellationToken)
    {
        var module = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (module is null)
        {
            return Result.Failure<ModuleDetailsDto>(ModulesError.NotFound);
        }

        module.Translations = command.Translations.ToDictionary(
            translation => translation.Key,
            translation => new ModuleTranslation
            {
                Name = translation.Value.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(translation.Value.Description)
                    ? null
                    : translation.Value.Description.Trim()
            },
            StringComparer.Ordinal);
        var spanish = module.Translations["es"];
        module.Name = spanish.Name;
        module.Description = spanish.Description;
        module.Icon = command.Icon;
        module.SortOrder = command.SortOrder;
        await _repository.SaveChangesAsync(cancellationToken);
        _permissionService.InvalidateAll();

        await _auditLogger.LogAsync(
            "module.updated",
            "module",
            module.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["moduleId"] = module.Id,
                ["key"] = module.Key,
                ["icon"] = module.Icon,
                ["sortOrder"] = module.SortOrder
            },
            cancellationToken);

        var updated = await _repository.GetModuleAsync(module.Id, cancellationToken)
            ?? throw new InvalidOperationException("The updated module could not be loaded.");
        return Result.Success(updated);
    }
}

public sealed class SetModuleStatusHandler : ICommandHandler<SetModuleStatusCommand>
{
    private readonly IModulesRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public SetModuleStatusHandler(
        IModulesRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(SetModuleStatusCommand command, CancellationToken cancellationToken)
    {
        var module = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (module is null)
        {
            return Result.Failure(ModulesError.NotFound);
        }
        if (!command.IsActive && (module.IsSystem || module.Key is "dashboard" or "profile"))
        {
            return Result.Failure(ModulesError.ProtectedModule);
        }

        module.IsActive = command.IsActive;
        await _repository.SaveChangesAsync(cancellationToken);
        _permissionService.InvalidateAll();
        await _auditLogger.LogAsync(
            "module.status-changed",
            "module",
            module.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["moduleId"] = module.Id,
                ["key"] = module.Key,
                ["isActive"] = module.IsActive
            },
            cancellationToken);
        return Result.Success();
    }
}
