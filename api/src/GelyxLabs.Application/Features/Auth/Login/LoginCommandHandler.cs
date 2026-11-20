namespace GelyxLabs.Application.Features.Auth.Login;

using GelyxLabs.Domain.Errors;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IMemoryCache _cache;

    public LoginCommandHandler(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IMemoryCache cache)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _cache = cache;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user is null)
        {
            return Result.Failure<AuthResponse>(new Error("invalid_credentials", "Credenciales inválidas"));
        }

        if (user.IsDeleted)
        {
            return Result.Failure<AuthResponse>(new Error("invalid_credentials", "Credenciales inválidas"));
        }

        if (!user.IsActive)
        {
            var signInResultActive = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: false);
            if (!signInResultActive.Succeeded)
            {
                return Result.Failure<AuthResponse>(new Error("invalid_credentials", "Credenciales inválidas"));
            }
            return Result.Failure<AuthResponse>(new Error("account_inactive", "La cuenta está inactiva"));
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return Result.Failure<AuthResponse>(new Error("account_locked", "La cuenta está bloqueada temporalmente por intentos fallidos"));
        }

        if (!signInResult.Succeeded)
        {
            return Result.Failure<AuthResponse>(new Error("invalid_credentials", "Credenciales inválidas"));
        }

        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            var twoFactorToken = _tokenService.GenerateTwoFactorToken(user.Id);
            _cache.Set($"two-factor:{twoFactorToken}", true, TimeSpan.FromMinutes(5));
            return Result.Success(new AuthResponse(null, null, 0, true, twoFactorToken));
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email ?? string.Empty, roles);
        var refreshResult = await _refreshTokenService.CreateAsync(user.Id, null);

        return Result.Success(new AuthResponse(
            accessToken,
            refreshResult.RefreshToken!,
            15 * 60
        ));
    }
}