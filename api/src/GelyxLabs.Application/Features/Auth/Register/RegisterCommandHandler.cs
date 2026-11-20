namespace GelyxLabs.Application.Features.Auth.Register;
using GelyxLabs.Domain.Errors;
using GelyxLabs.Application.Options;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Application.Abstractions.Settings;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AuthOptions _authOptions;
    private readonly IPermissionService _permissionService;
    private readonly IAppSettingsService _settings;

    public RegisterCommandHandler(
        UserManager<AppUser> userManager,
        IOptions<AuthOptions> authOptions,
        IPermissionService permissionService,
        IAppSettingsService settings)
    {
        _userManager = userManager;
        _authOptions = authOptions.Value;
        _permissionService = permissionService;
        _settings = settings;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (!await _settings.GetBooleanAsync("auth.allowSelfRegistration", _authOptions.AllowSelfRegistration, cancellationToken))
        {
            return Result.Failure<RegisterResponse>(new Error("registration_disabled", "El registro está deshabilitado"));
        }

        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
        {
            return Result.Failure<RegisterResponse>(Error.Conflict);
        }

        var user = new AppUser
        {
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = true,
            FullName = command.FullName,
            IsActive = true,
            MustChangePassword = false
        };

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<RegisterResponse>(new Error("registration_failed", error));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Usuario");
        if (roleResult.Succeeded)
        {
            _permissionService.InvalidateUser(user.Id);
        }

        return Result.Success(new RegisterResponse(user.Id, user.Email, user.FullName));
    }
}
