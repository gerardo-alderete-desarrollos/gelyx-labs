namespace GelyxLabs.Application.Features.Roles;

using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;

public sealed class GetRolesHandler : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleListItemDto>>
{
    private readonly IRolesRepository _repository;

    public GetRolesHandler(IRolesRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<RoleListItemDto>>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken) =>
        Result.Success(await _repository.GetRolesAsync(cancellationToken));
}

public sealed class GetRoleByIdHandler : IQueryHandler<GetRoleByIdQuery, RoleDetailsDto>
{
    private readonly IRolesRepository _repository;

    public GetRoleByIdHandler(IRolesRepository repository) => _repository = repository;

    public async Task<Result<RoleDetailsDto>> Handle(
        GetRoleByIdQuery query,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleAsync(query.Id, cancellationToken);
        return role is null
            ? Result.Failure<RoleDetailsDto>(RolesError.NotFound)
            : Result.Success(role);
    }
}

public sealed class CreateRoleHandler : ICommandHandler<CreateRoleCommand, RoleDetailsDto>
{
    private readonly IRolesRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public CreateRoleHandler(
        IRolesRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<RoleDetailsDto>> Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        if (!RolesBusinessRules.IsValidName(name))
        {
            return Result.Failure<RoleDetailsDto>(RolesError.RoleNameInvalid);
        }
        if (await _repository.NameExistsAsync(name, null, cancellationToken))
        {
            return Result.Failure<RoleDetailsDto>(RolesError.RoleNameTaken);
        }

        IReadOnlyList<string> permissions;
        if (command.CopyPermissionsFromRoleId is { } sourceRoleId)
        {
            if (await _repository.FindByIdAsync(sourceRoleId, cancellationToken) is null)
            {
                return Result.Failure<RoleDetailsDto>(RolesError.CopySourceNotFound);
            }
            permissions = await _repository.GetCopyablePermissionsAsync(sourceRoleId, cancellationToken);
        }
        else
        {
            permissions = await _repository.GetCopyablePermissionsAsync(null, cancellationToken);
        }

        var role = new AppRole
        {
            Name = name,
            Description = command.Description?.Trim(),
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var createResult = await _repository.CreateAsync(role, permissions, cancellationToken);
        if (!createResult.Succeeded)
        {
            return Result.Failure<RoleDetailsDto>(RolesError.RoleNameTaken);
        }

        _permissionService.InvalidateAll();
        var details = await _repository.GetRoleAsync(role.Id, cancellationToken)
            ?? throw new InvalidOperationException("The newly created role could not be loaded.");
        await _auditLogger.LogAsync(
            "role.created",
            "role",
            role.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["roleId"] = role.Id,
                ["name"] = role.Name,
                ["permissionCount"] = details.PermissionCount
            },
            cancellationToken);
        return Result.Success(details);
    }
}

public sealed class UpdateRoleHandler : ICommandHandler<UpdateRoleCommand, RoleDetailsDto>
{
    private readonly IRolesRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public UpdateRoleHandler(
        IRolesRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<RoleDetailsDto>> Handle(
        UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleDetailsDto>(RolesError.NotFound);
        }
        var name = command.Name.Trim();
        if (role.IsSystem && !string.Equals(name, role.Name, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RoleDetailsDto>(RolesError.SystemRoleProtected);
        }

        if (!role.IsSystem && !RolesBusinessRules.IsValidName(name))
        {
            return Result.Failure<RoleDetailsDto>(RolesError.RoleNameInvalid);
        }
        if (!role.IsSystem && await _repository.NameExistsAsync(name, role.Id, cancellationToken))
        {
            return Result.Failure<RoleDetailsDto>(RolesError.RoleNameTaken);
        }

        if (!role.IsSystem)
        {
            role.Name = name;
        }
        role.Description = command.Description?.Trim();
        var updateResult = await _repository.UpdateAsync(role, cancellationToken);
        if (!updateResult.Succeeded)
        {
            return Result.Failure<RoleDetailsDto>(RolesError.RoleNameTaken);
        }

        _permissionService.InvalidateAll();
        var details = await _repository.GetRoleAsync(role.Id, cancellationToken)
            ?? throw new InvalidOperationException("The updated role could not be loaded.");
        await _auditLogger.LogAsync(
            "role.updated",
            "role",
            role.Id.ToString(),
            new Dictionary<string, object?> { ["roleId"] = role.Id, ["name"] = role.Name },
            cancellationToken);
        return Result.Success(details);
    }
}

public sealed class DeleteRoleHandler : ICommandHandler<DeleteRoleCommand>
{
    private readonly IRolesRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public DeleteRoleHandler(
        IRolesRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RolesError.NotFound);
        }
        if (role.IsSystem)
        {
            return Result.Failure(RolesError.SystemRoleProtected);
        }
        if (command.ReassignToRoleId is { } targetId)
        {
            var target = await _repository.FindByIdAsync(targetId, cancellationToken);
            if (target is null)
            {
                return Result.Failure(RolesError.InvalidReassignment);
            }
        }

        var result = await _repository.DeleteAsync(command.Id, command.ReassignToRoleId, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure(result.ErrorCode switch
            {
                "role_has_users" => RolesError.RoleHasUsers,
                "role_not_found" => RolesError.NotFound,
                "invalid_reassignment" => RolesError.InvalidReassignment,
                _ => RolesError.OperationFailed
            });
        }

        _permissionService.InvalidateAll();
        await _auditLogger.LogAsync(
            "role.deleted",
            "role",
            role.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["roleId"] = role.Id,
                ["name"] = role.Name,
                ["userCount"] = result.UserCount,
                ["reassignToRoleId"] = command.ReassignToRoleId
            },
            cancellationToken);
        return Result.Success();
    }
}

public static class RolesBusinessRules
{
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name.Trim().Length is >= 3 and <= 50 &&
        !string.Equals(name.Trim(), "Admin", StringComparison.OrdinalIgnoreCase);
}
