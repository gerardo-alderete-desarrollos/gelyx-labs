namespace GelyxLabs.Application.Features.Users;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public sealed record GetUsersQuery(
    int Page,
    int PageSize,
    string? Search,
    Guid? RoleId,
    string? Status,
    string? SortBy,
    string? SortDir) : IQuery<UsersPageDto>;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserDetailsDto>;
public sealed record GetUsersSummaryQuery : IQuery<UsersSummaryDto>;
public sealed record CreateUserCommand(string Email, string FullName, Guid[] RoleIds) : ICommand<CreateUserResponse>;
public sealed record UpdateUserCommand(Guid Id, string Email, string FullName, string? PhoneNumber, uint RowVersion) : ICommand;
public sealed record SetUserStatusCommand(Guid Id, bool IsActive) : ICommand;
public sealed record AssignUserRolesCommand(Guid Id, Guid[] RoleIds) : ICommand;
public sealed record ResetUserPasswordCommand(Guid Id) : ICommand<string>;
public sealed record DeleteUserCommand(Guid Id) : ICommand;
public sealed record RestoreUserCommand(Guid Id) : ICommand;
public sealed record CreateUserResponse(Guid Id, string Email, string FullName, string TemporaryPassword);
public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string? FullName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset? LastLoginAt,
    uint RowVersion);
public sealed record UserDetailsDto(
    Guid Id,
    string Email,
    string? FullName,
    string? PhoneNumber,
    IReadOnlyList<UserRoleDto> Roles,
    bool IsActive,
    bool IsDeleted,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    uint RowVersion);
public sealed record UserRoleDto(Guid Id, string Name);
public sealed record UsersPageDto(
    IReadOnlyList<UserListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
public sealed record UsersSummaryDto(
    int Total,
    int Active,
    int Inactive,
    int Deleted,
    IReadOnlyList<RecentUserLoginDto> RecentLogins);
public sealed record RecentUserLoginDto(Guid Id, string Email, string? FullName, DateTimeOffset LastLoginAt);

public interface IUsersRepository
{
    Task<UsersPageDto> GetUsersAsync(GetUsersQuery query, CancellationToken cancellationToken);
    IAsyncEnumerable<UserListItemDto> StreamUsersAsync(GetUsersQuery query, CancellationToken cancellationToken);
    Task<UserDetailsDto?> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<UsersSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    Task<AppUser?> GetTrackedUserAsync(Guid id, bool includeDeleted, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? exceptUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AppRole>> GetRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken);
    Task SetUserRolesAsync(
        AppUser user,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken);
    Task<IdentityResult> UpdateUserAsync(AppUser user, uint? expectedRowVersion, CancellationToken cancellationToken);
    Task<IdentityResult> CreateUserAsync(AppUser user, string password, CancellationToken cancellationToken);
}

public static class UsersError
{
    public static readonly GelyxLabs.Domain.Errors.Error NotFound = new("user_not_found", "User was not found.");
    public static readonly GelyxLabs.Domain.Errors.Error InvalidRoles = new("invalid_roles", "One or more roles were not found.");
    public static readonly GelyxLabs.Domain.Errors.Error SelfAction = new("self_action_forbidden", "You cannot perform this action on your own account.");
    public static readonly GelyxLabs.Domain.Errors.Error LastAdmin = new("last_admin_protected", "The last active Admin cannot be deactivated, deleted, or unassigned from the Admin role.");
    public static readonly GelyxLabs.Domain.Errors.Error EmailTaken = new("email_taken", "This email address is already in use.");
    public static readonly GelyxLabs.Domain.Errors.Error Concurrency = new("concurrency_conflict", "The user was modified by another request.");
    public static readonly GelyxLabs.Domain.Errors.Error OperationFailed = new("user_operation_failed", "The user operation could not be completed.");
}
