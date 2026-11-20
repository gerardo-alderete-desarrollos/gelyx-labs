namespace GelyxLabs.Application.Features.Auth.Me;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Domain.Errors;
using Microsoft.AspNetCore.Identity;

public sealed class UpdateUserPreferencesCommandHandler : ICommandHandler<UpdateUserPreferencesCommand>
{
    private static readonly HashSet<string> SupportedThemes = new(StringComparer.Ordinal)
    {
        "light",
        "dark",
        "system"
    };

    private static readonly HashSet<string> SupportedLocales = new(StringComparer.Ordinal)
    {
        "es",
        "en"
    };

    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserPreferencesCommandHandler(
        UserManager<AppUser> userManager,
        ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateUserPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        if (!SupportedLocales.Contains(command.Locale))
        {
            return Result.Failure(new Error(
                "unsupported_locale",
                "The requested locale is not supported."));
        }

        if (command.Theme is not null && !SupportedThemes.Contains(command.Theme))
        {
            return Result.Failure(new Error(
                "unsupported_theme",
                "The requested theme is not supported."));
        }

        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(new Error("unauthorized", "Not authenticated."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Failure(new Error("unauthorized", "User was not found or is inactive."));
        }

        user.Locale = command.Locale;
        if (command.Theme is not null)
        {
            user.Theme = command.Theme;
        }
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(new Error("preferences_update_failed", "The preferences could not be updated."));
    }
}
