namespace GelyxLabs.Application.Features.Profile;

using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Domain.Errors;
using Microsoft.AspNetCore.Identity;

public sealed class UpdateOwnProfileCommandHandler : ICommandHandler<UpdateOwnProfileCommand>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public UpdateOwnProfileCommandHandler(
        UserManager<AppUser> userManager,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(
        UpdateOwnProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(new Error("unauthorized", "No autenticado."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Failure(new Error("unauthorized", "Usuario no encontrado o inactivo."));
        }

        user.FullName = command.FullName.Trim();
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Failure(new Error(
                "profile_update_failed",
                "No se pudo actualizar el perfil."));
        }

        await _auditLogger.LogAsync(
            "profile.updated",
            "user",
            userId.ToString(),
            new Dictionary<string, object?> { ["userId"] = userId },
            cancellationToken);

        return Result.Success();
    }
}
