namespace GelyxLabs.Application.Features.Permissions;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;

public sealed class UpdateRolePermissionsHandler : ICommandHandler<UpdateRolePermissionsCommand>
{
    private readonly IPermissionsRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public UpdateRolePermissionsHandler(
        IPermissionsRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var result = await _repository.UpdateRolePermissionsAsync(
            command.RoleId,
            command.Permissions,
            cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        _permissionService.InvalidateAll();
        await _auditLogger.LogAsync(
            "permissions.role-updated",
            "role",
            command.RoleId.ToString(),
            new Dictionary<string, object?>
            {
                ["roleId"] = command.RoleId,
                ["granted"] = result.Value.Granted,
                ["removed"] = result.Value.Removed
            },
            cancellationToken);
        return Result.Success();
    }
}
