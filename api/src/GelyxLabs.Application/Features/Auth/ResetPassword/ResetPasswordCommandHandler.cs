namespace GelyxLabs.Application.Features.Auth.ResetPassword;
using GelyxLabs.Domain.Errors;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Common;
using Microsoft.AspNetCore.Identity;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenService _tokenService;

    public ResetPasswordCommandHandler(
        UserManager<AppUser> userManager,
        IRefreshTokenService refreshTokenService,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _tokenService = tokenService;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Failure(new Error("invalid_token", "Token inválido o expirado"));
        }

        var token = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(command.Token));
        var result = await _userManager.ResetPasswordAsync(user, token, command.NewPassword);
        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("reset_failed", error));
        }

        await _refreshTokenService.RevokeAllAsync(user.Id, "password_reset", cancellationToken);

        return Result.Success();
    }
}
