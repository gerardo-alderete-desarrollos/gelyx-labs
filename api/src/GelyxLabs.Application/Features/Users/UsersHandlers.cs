namespace GelyxLabs.Application.Features.Users;

using System.Security.Cryptography;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using static GelyxLabs.Application.Features.Users.UserHandlerHelpers;

public sealed class GetUsersHandler : IQueryHandler<GetUsersQuery, UsersPageDto>
{
    private readonly IUsersRepository _repository;

    public GetUsersHandler(IUsersRepository repository) => _repository = repository;

    public async Task<Result<UsersPageDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken) =>
        Result.Success(await _repository.GetUsersAsync(query, cancellationToken));
}

public sealed class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, UserDetailsDto>
{
    private readonly IUsersRepository _repository;

    public GetUserByIdHandler(IUsersRepository repository) => _repository = repository;

    public async Task<Result<UserDetailsDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserAsync(query.Id, cancellationToken);
        return user is null
            ? Result.Failure<UserDetailsDto>(UsersError.NotFound)
            : Result.Success(user);
    }
}

public sealed class GetUsersSummaryHandler : IQueryHandler<GetUsersSummaryQuery, UsersSummaryDto>
{
    private readonly IUsersRepository _repository;

    public GetUsersSummaryHandler(IUsersRepository repository) => _repository = repository;

    public async Task<Result<UsersSummaryDto>> Handle(GetUsersSummaryQuery query, CancellationToken cancellationToken) =>
        Result.Success(await _repository.GetSummaryAsync(cancellationToken));
}

public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUsersRepository _repository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailQueue _emailQueue;
    private readonly IAuditLogger _auditLogger;
    private readonly IPermissionService _permissionService;

    public CreateUserHandler(
        IUsersRepository repository,
        UserManager<AppUser> userManager,
        IEmailQueue emailQueue,
        IAuditLogger auditLogger,
        IPermissionService permissionService)
    {
        _repository = repository;
        _userManager = userManager;
        _emailQueue = emailQueue;
        _auditLogger = auditLogger;
        _permissionService = permissionService;
    }

    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.EmailExistsAsync(command.Email, null, cancellationToken))
        {
            return Result.Failure<CreateUserResponse>(UsersError.EmailTaken);
        }

        var roles = await _repository.GetRolesAsync(command.RoleIds.Distinct().ToArray(), cancellationToken);
        if (roles.Count != command.RoleIds.Distinct().Count())
        {
            return Result.Failure<CreateUserResponse>(UsersError.InvalidRoles);
        }

        var password = GenerateTemporaryPassword();
        var user = new AppUser
        {
            UserName = command.Email.Trim(),
            Email = command.Email.Trim(),
            EmailConfirmed = true,
            FullName = command.FullName.Trim(),
            IsActive = true,
            MustChangePassword = true
        };
        var createResult = await _repository.CreateUserAsync(user, password, cancellationToken);
        if (!createResult.Succeeded)
        {
            return Result.Failure<CreateUserResponse>(UserHandlerHelpers.MapIdentityError(createResult));
        }
        var addRolesResult = await _userManager.AddToRolesAsync(user, roles.Select(role => role.Name!));
        if (!addRolesResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Result.Failure<CreateUserResponse>(UserHandlerHelpers.MapIdentityError(addRolesResult));
        }

        await _emailQueue.EnqueueAsync(
            EmailTemplates.TemporaryPasswordWelcome(user.Email!, user.Locale, password),
            cancellationToken);
        _permissionService.InvalidateUser(user.Id);
        await _auditLogger.LogAsync(
            "user.created",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id, ["roleIds"] = command.RoleIds.Distinct().ToArray() },
            cancellationToken);
        return Result.Success(new CreateUserResponse(user.Id, user.Email!, user.FullName!, password));
    }

    private static string GenerateTemporaryPassword()
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', 'A')
            .Replace('/', 'b')
            .TrimEnd('=');
        return $"{random}Aa1!";
    }

}

public sealed class UpdateUserHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUsersRepository _repository;
    private readonly IAuditLogger _auditLogger;

    public UpdateUserHandler(IUsersRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetTrackedUserAsync(command.Id, false, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UsersError.NotFound);
        }
        if (await _repository.EmailExistsAsync(command.Email, command.Id, cancellationToken))
        {
            return Result.Failure(UsersError.EmailTaken);
        }

        user.Email = command.Email.Trim();
        user.UserName = command.Email.Trim();
        user.FullName = command.FullName.Trim();
        user.PhoneNumber = command.PhoneNumber?.Trim();
        var result = await _repository.UpdateUserAsync(user, command.RowVersion, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure(IsConcurrencyFailure(result)
                ? UsersError.Concurrency
                : result.Errors.Any(error => error.Code == "DuplicateEmail")
                    ? UsersError.EmailTaken
                    : new GelyxLabs.Domain.Errors.Error("user_operation_failed", JoinErrors(result)));
        }

        await _auditLogger.LogAsync(
            "user.updated",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id },
            cancellationToken);
        return Result.Success();
    }
}

public sealed class SetUserStatusHandler : ICommandHandler<SetUserStatusCommand>
{
    private readonly IUsersRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public SetUserStatusHandler(
        IUsersRepository repository,
        ICurrentUserService currentUser,
        UserManager<AppUser> userManager,
        IRefreshTokenService refreshTokenService,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(SetUserStatusCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetTrackedUserAsync(command.Id, false, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UsersError.NotFound);
        }
        if (!command.IsActive && UsersBusinessRules.IsSelfAction(_currentUser.UserId, user.Id))
        {
            return Result.Failure(UsersError.SelfAction);
        }
        if (!command.IsActive && await IsLastActiveAdminAsync(user, cancellationToken))
        {
            return Result.Failure(UsersError.LastAdmin);
        }

        user.IsActive = command.IsActive;
        var result = await _repository.UpdateUserAsync(user, null, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure(IsConcurrencyFailure(result)
                ? UsersError.Concurrency
                : new GelyxLabs.Domain.Errors.Error("user_operation_failed", JoinErrors(result)));
        }

        if (!command.IsActive)
        {
            await _refreshTokenService.RevokeAllAsync(user.Id, "user_deactivated", cancellationToken);
        }
        _permissionService.InvalidateUser(user.Id);
        await _auditLogger.LogAsync(
            "user.status-changed",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id, ["isActive"] = command.IsActive },
            cancellationToken);
        return Result.Success();
    }

    private async Task<bool> IsLastActiveAdminAsync(AppUser user, CancellationToken cancellationToken) =>
        user.IsActive &&
        (await _userManager.GetRolesAsync(user)).Contains("Admin", StringComparer.Ordinal) &&
        await _repository.CountActiveAdminsAsync(cancellationToken) <= 1;
}

public sealed class AssignUserRolesHandler : ICommandHandler<AssignUserRolesCommand>
{
    private readonly IUsersRepository _repository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public AssignUserRolesHandler(
        IUsersRepository repository,
        UserManager<AppUser> userManager,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _userManager = userManager;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(AssignUserRolesCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetTrackedUserAsync(command.Id, false, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UsersError.NotFound);
        }
        var requestedIds = command.RoleIds.Distinct().ToArray();
        var roles = await _repository.GetRolesAsync(requestedIds, cancellationToken);
        if (roles.Count != requestedIds.Length)
        {
            return Result.Failure(UsersError.InvalidRoles);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var requestedNames = roles.Select(role => role.Name!).ToArray();
        if (UsersBusinessRules.WouldRemoveLastActiveAdmin(
            user.IsActive,
            currentRoles.Contains("Admin", StringComparer.Ordinal) &&
                !requestedNames.Contains("Admin", StringComparer.Ordinal),
            await _repository.CountActiveAdminsAsync(cancellationToken)))
        {
            return Result.Failure(UsersError.LastAdmin);
        }

        await _repository.SetUserRolesAsync(user, requestedIds, cancellationToken);

        _permissionService.InvalidateUser(user.Id);
        await _auditLogger.LogAsync(
            "user.roles-changed",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id, ["roleIds"] = requestedIds },
            cancellationToken);
        return Result.Success();
    }
}

public sealed class ResetUserPasswordHandler : ICommandHandler<ResetUserPasswordCommand, string>
{
    private readonly IUsersRepository _repository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuditLogger _auditLogger;

    public ResetUserPasswordHandler(
        IUsersRepository repository,
        UserManager<AppUser> userManager,
        IRefreshTokenService refreshTokenService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<string>> Handle(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetTrackedUserAsync(command.Id, false, cancellationToken);
        if (user is null)
        {
            return Result.Failure<string>(UsersError.NotFound);
        }

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', 'A').Replace('/', 'b').TrimEnd('=') + "Aa1!";
        user.MustChangePassword = true;
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, password);
        if (!result.Succeeded)
        {
            return Result.Failure<string>(new GelyxLabs.Domain.Errors.Error("user_operation_failed", JoinErrors(result)));
        }

        await _refreshTokenService.RevokeAllAsync(user.Id, "password_reset_by_admin", cancellationToken);
        await _auditLogger.LogAsync(
            "user.password-reset",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id },
            cancellationToken);
        return Result.Success(password);
    }
}

public sealed class DeleteUserHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUsersRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public DeleteUserHandler(
        IUsersRepository repository,
        ICurrentUserService currentUser,
        IRefreshTokenService refreshTokenService,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _refreshTokenService = refreshTokenService;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetTrackedUserAsync(command.Id, false, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UsersError.NotFound);
        }
        if (UsersBusinessRules.IsSelfAction(_currentUser.UserId, user.Id))
        {
            return Result.Failure(UsersError.SelfAction);
        }
        var roles = await _repository.GetUserRoleNamesAsync(user.Id, cancellationToken);
        if (UsersBusinessRules.WouldRemoveLastActiveAdmin(
            user.IsActive,
            roles.Contains("Admin", StringComparer.Ordinal),
            await _repository.CountActiveAdminsAsync(cancellationToken)))
        {
            return Result.Failure(UsersError.LastAdmin);
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.DeletedBy = _currentUser.UserId;
        var result = await _repository.UpdateUserAsync(user, null, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure(IsConcurrencyFailure(result)
                ? UsersError.Concurrency
                : new GelyxLabs.Domain.Errors.Error("user_operation_failed", JoinErrors(result)));
        }

        await _refreshTokenService.RevokeAllAsync(user.Id, "user_deleted", cancellationToken);
        _permissionService.InvalidateUser(user.Id);
        await _auditLogger.LogAsync(
            "user.deleted",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id },
            cancellationToken);
        return Result.Success();
    }
}

public sealed class RestoreUserHandler : ICommandHandler<RestoreUserCommand>
{
    private readonly IUsersRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogger _auditLogger;

    public RestoreUserHandler(
        IUsersRepository repository,
        IPermissionService permissionService,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(RestoreUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetTrackedUserAsync(command.Id, true, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UsersError.NotFound);
        }
        if (!user.IsDeleted)
        {
            return Result.Failure(new GelyxLabs.Domain.Errors.Error("user_not_deleted", "User is not deleted."));
        }
        if (await _repository.EmailExistsAsync(user.Email!, user.Id, cancellationToken))
        {
            return Result.Failure(UsersError.EmailTaken);
        }

        user.IsDeleted = false;
        user.IsActive = true;
        user.DeletedAt = null;
        user.DeletedBy = null;
        var result = await _repository.UpdateUserAsync(user, null, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure(IsConcurrencyFailure(result)
                ? UsersError.Concurrency
                : new GelyxLabs.Domain.Errors.Error("user_operation_failed", JoinErrors(result)));
        }

        _permissionService.InvalidateUser(user.Id);
        await _auditLogger.LogAsync(
            "user.restored",
            "user",
            user.Id.ToString(),
            new Dictionary<string, object?> { ["userId"] = user.Id },
            cancellationToken);
        return Result.Success();
    }
}

internal static class UserHandlerHelpers
{
    public static bool IsConcurrencyFailure(IdentityResult result) =>
        result.Errors.Any(error => error.Code == "ConcurrencyFailure");

    public static string JoinErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));

    public static GelyxLabs.Domain.Errors.Error MapIdentityError(IdentityResult result) =>
        result.Errors.Any(error => error.Code == "DuplicateEmail")
            ? UsersError.EmailTaken
            : new("user_operation_failed", JoinErrors(result));
}
