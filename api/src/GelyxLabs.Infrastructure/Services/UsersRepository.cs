using GelyxLabs.Application.Features.Users;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Runtime.CompilerServices;

namespace GelyxLabs.Infrastructure.Services;

public sealed class UsersRepository : IUsersRepository
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public UsersRepository(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UsersPageDto> GetUsersAsync(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = BuildUsersQuery(query);
        var totalCount = await users.CountAsync(cancellationToken);
        var results = await users
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var userIds = results.Select(user => user.Id).ToArray();
        var roleNames = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);
        var rolesByUser = roleNames
            .GroupBy(link => link.UserId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group
                .Select(link => link.Name!)
                .OrderBy(name => name)
                .ToArray());

        return new UsersPageDto(
            results.Select(user => ToListItem(user, rolesByUser.GetValueOrDefault(user.Id, []))).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    public async IAsyncEnumerable<UserListItemDto> StreamUsersAsync(
        GetUsersQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var user in BuildUsersQuery(query).Take(10_000).AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            var roles = await GetUserRoleNamesAsync(user.Id, cancellationToken);
            yield return ToListItem(user, roles);
        }
    }

    private IQueryable<AppUser> BuildUsersQuery(GetUsersQuery query)
    {
        var users = _context.Users.IgnoreQueryFilters().AsNoTracking();
        users = query.Status switch
        {
            "active" => users.Where(user => !user.IsDeleted && user.IsActive),
            "inactive" => users.Where(user => !user.IsDeleted && !user.IsActive),
            "deleted" => users.Where(user => user.IsDeleted),
            _ => users.Where(user => !user.IsDeleted)
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            users = users.Where(user =>
                (user.NormalizedEmail != null && user.NormalizedEmail.Contains(search)) ||
                (user.FullName != null && user.FullName.ToUpper().Contains(search)));
        }
        if (query.RoleId is { } roleId)
        {
            users = users.Where(user => _context.UserRoles.Any(link => link.UserId == user.Id && link.RoleId == roleId));
        }

        users = (query.SortBy?.ToLowerInvariant(), query.SortDir?.ToLowerInvariant()) switch
        {
            ("email", "asc") => users.OrderBy(user => user.NormalizedEmail).ThenBy(user => user.Id),
            ("email", "desc") => users.OrderByDescending(user => user.NormalizedEmail).ThenBy(user => user.Id),
            ("lastloginat", "asc") => users.OrderBy(user => user.LastLoginAt).ThenBy(user => user.Id),
            ("lastloginat", "desc") => users.OrderByDescending(user => user.LastLoginAt).ThenBy(user => user.Id),
            ("name", "desc") => users.OrderByDescending(user => user.FullName).ThenBy(user => user.Id),
            ("name", _) => users.OrderBy(user => user.FullName).ThenBy(user => user.Id),
            (_, "desc") => users.OrderByDescending(user => user.FullName).ThenBy(user => user.Id),
            _ => users.OrderBy(user => user.FullName).ThenBy(user => user.Id)
        };

        return users;
    }

    private static UserListItemDto ToListItem(AppUser user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.Email!,
            user.FullName,
            user.PhoneNumber,
            roles,
            user.IsActive,
            user.IsDeleted,
            user.LastLoginAt,
            user.RowVersion);

    public async Task<UserDetailsDto?> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == id
            orderby role.Name
            select new UserRoleDto(role.Id, role.Name!))
            .ToArrayAsync(cancellationToken);
        return new UserDetailsDto(
            user.Id,
            user.Email!,
            user.FullName,
            user.PhoneNumber,
            roles,
            user.IsActive,
            user.IsDeleted,
            user.MustChangePassword,
            user.LastLoginAt,
            user.RowVersion);
    }

    public async Task<UsersSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var users = _context.Users.IgnoreQueryFilters().AsNoTracking();
        var total = await users.CountAsync(cancellationToken);
        var active = await users.CountAsync(user => !user.IsDeleted && user.IsActive, cancellationToken);
        var inactive = await users.CountAsync(user => !user.IsDeleted && !user.IsActive, cancellationToken);
        var deleted = await users.CountAsync(user => user.IsDeleted, cancellationToken);
        var recentUsers = await users
            .Where(user => !user.IsDeleted && user.LastLoginAt != null)
            .OrderByDescending(user => user.LastLoginAt)
            .Take(5)
            .Select(user => new { user.Id, user.Email, user.FullName, LastLoginAt = user.LastLoginAt!.Value })
            .ToArrayAsync(cancellationToken);
        return new UsersSummaryDto(
            total,
            active,
            inactive,
            deleted,
            recentUsers.Select(user => new RecentUserLoginDto(
                user.Id,
                user.Email!,
                user.FullName,
                user.LastLoginAt)).ToArray());
    }

    public Task<AppUser?> GetTrackedUserAsync(Guid id, bool includeDeleted, CancellationToken cancellationToken)
    {
        var users = _context.Users.IgnoreQueryFilters();
        if (!includeDeleted)
        {
            users = users.Where(user => !user.IsDeleted);
        }
        return users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var users = _context.Users.IgnoreQueryFilters().Where(user => user.NormalizedEmail == normalizedEmail);
        if (exceptUserId is { } userId)
        {
            users = users.Where(user => user.Id != userId);
        }
        return users.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppRole>> GetRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }
        return await _context.Roles.Where(role => roleIds.Contains(role.Id)).ToArrayAsync(cancellationToken);
    }

    public async Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken)
    {
        var adminRoleId = await GetAdminRoleIdAsync(cancellationToken);
        if (adminRoleId is null)
        {
            return 0;
        }
        return await (
            from user in _context.Users.IgnoreQueryFilters()
            join link in _context.UserRoles on user.Id equals link.UserId
            where link.RoleId == adminRoleId && !user.IsDeleted && user.IsActive
            select user.Id)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken) =>
        await (
            from link in _context.UserRoles
            join role in _context.Roles on link.RoleId equals role.Id
            where link.UserId == userId
            select role.Name!)
            .ToArrayAsync(cancellationToken);

    private Task<Guid?> GetAdminRoleIdAsync(CancellationToken cancellationToken) =>
        _context.Roles
            .Where(role => role.Name == "Admin")
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IdentityResult> UpdateUserAsync(
        AppUser user,
        uint? expectedRowVersion,
        CancellationToken cancellationToken)
    {
        if (expectedRowVersion is { } rowVersion && user.RowVersion != rowVersion)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "The user was modified by another request."
            });
        }
        try
        {
            return await _userManager.UpdateAsync(user);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "The user was modified by another request."
            });
        }
        catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
        {
            return IdentityResult.Failed(_userManager.ErrorDescriber.DuplicateEmail(user.Email!));
        }
    }

    public async Task<IdentityResult> CreateUserAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
        {
            return IdentityResult.Failed(_userManager.ErrorDescriber.DuplicateEmail(user.Email!));
        }
    }

    public async Task SetUserRolesAsync(
        AppUser user,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var existing = await _context.UserRoles
            .Where(link => link.UserId == user.Id)
            .ToArrayAsync(cancellationToken);
        _context.UserRoles.RemoveRange(existing);
        foreach (var roleId in roleIds)
        {
            _context.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = roleId
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsEmailUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "EmailIndex"
        };
}
