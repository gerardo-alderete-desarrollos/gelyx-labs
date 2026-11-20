namespace GelyxLabs.Application.Features.Auth.Me;
using GelyxLabs.Domain.Errors;

using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Modules;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public sealed class MeQueryHandler : IQueryHandler<MeQuery, MeResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly IPermissionService _permissionService;

    public MeQueryHandler(
        UserManager<AppUser> userManager,
        ICurrentUserService currentUserService,
        IModuleCatalog moduleCatalog,
        IPermissionService permissionService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _moduleCatalog = moduleCatalog;
        _permissionService = permissionService;
    }

    public async Task<Result<MeResponse>> Handle(MeQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<MeResponse>(new Error("unauthorized", "No autenticado"));
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Failure<MeResponse>(new Error("unauthorized", "Usuario no encontrado o inactivo"));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var permissions = (await _permissionService.GetPermissionsAsync(user.Id, cancellationToken)).ToArray();
        var isAdmin = roles.Contains("Admin", StringComparer.Ordinal);
        var activeModules = await _moduleCatalog.GetActiveAsync(cancellationToken);
        var modules = activeModules
            .Where(module =>
                (isAdmin || !module.IsSystem) &&
                (isAdmin || permissions.Contains($"{module.Key}.access", StringComparer.Ordinal)))
            .Select(module => new ModuleInfo(
                module.Key,
                module.LocalizedNames.TryGetValue(user.Locale, out var localizedName)
                    ? localizedName
                    : module.LocalizedNames.GetValueOrDefault("es", module.Name),
                module.Icon,
                module.Route,
                module.SortOrder))
            .ToArray();

        return Result.Success(new MeResponse(
            user.Id,
            user.Email!,
            user.FullName ?? string.Empty,
            [.. roles],
            permissions,
            modules,
            user.MustChangePassword,
            user.Locale,
            user.Theme
        ));
    }
}