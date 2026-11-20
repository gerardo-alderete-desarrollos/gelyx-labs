namespace GelyxLabs.Application.Features.Auth.ChangePassword;
using GelyxLabs.Domain.Errors;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Common;
using Microsoft.AspNetCore.Identity;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;

    public ChangePasswordCommandHandler(
        UserManager<AppUser> userManager,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure(new Error("unauthorized", "No autenticado"));
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Failure(new Error("unauthorized", "Usuario no encontrado o inactivo"));
        }

        var result = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("password_change_failed", error));
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        await _refreshTokenService.RevokeAllExceptAsync(user.Id, command.RefreshToken, "password_changed", cancellationToken);

        return Result.Success();
    }
}
