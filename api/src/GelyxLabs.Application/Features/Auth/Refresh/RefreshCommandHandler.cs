namespace GelyxLabs.Application.Features.Auth.Refresh;
using GelyxLabs.Domain.Errors;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Common;
using Microsoft.AspNetCore.Identity;

public sealed class RefreshCommandHandler : ICommandHandler<RefreshCommand, AuthResponse>
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly UserManager<AppUser> _userManager;

    public RefreshCommandHandler(
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        UserManager<AppUser> userManager)
    {
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _userManager = userManager;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        var rotateResult = await _refreshTokenService.RotateAsync(command.RefreshToken, null, cancellationToken);
        if (!rotateResult.Success)
        {
            var errorCode = rotateResult.ErrorCode ?? "token_invalid";
            return Result.Failure<AuthResponse>(new Error(errorCode, GetErrorMessage(errorCode)));
        }

        var user = await _userManager.FindByIdAsync(rotateResult.UserId.ToString());
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(new Error("token_invalid", "Token inválido"));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email ?? string.Empty, roles);

        return Result.Success(new AuthResponse(
            accessToken,
            rotateResult.RefreshToken!,
            15 * 60
        ));
    }

    private static string GetErrorMessage(string code)
    {
        return code switch
        {
            "token_reused" => "Token reutilizado. La sesión ha sido invalidada por seguridad.",
            "token_invalid" => "Token inválido o expirado",
            _ => "Token inválido"
        };
    }
}
