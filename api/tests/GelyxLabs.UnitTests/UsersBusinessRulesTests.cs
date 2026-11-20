using GelyxLabs.Application.Features.Users;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

namespace GelyxLabs.UnitTests;

public sealed class UsersBusinessRulesTests
{
    [Fact]
    public void IsSelfAction_OnlyMatchesTheTargetUser()
    {
        var userId = Guid.NewGuid();

        Assert.True(UsersBusinessRules.IsSelfAction(userId, userId));
        Assert.False(UsersBusinessRules.IsSelfAction(Guid.NewGuid(), userId));
        Assert.False(UsersBusinessRules.IsSelfAction(null, userId));
    }

    [Theory]
    [InlineData(true, true, 1, true)]
    [InlineData(true, true, 2, false)]
    [InlineData(false, true, 1, false)]
    [InlineData(true, false, 1, false)]
    public void WouldRemoveLastActiveAdmin_ProtectsOnlyTheLastActiveAdmin(
        bool isActive,
        bool hasAdminRole,
        int activeAdminCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            UsersBusinessRules.WouldRemoveLastActiveAdmin(isActive, hasAdminRole, activeAdminCount));
    }

    [Fact]
    public async Task DeleteUserHandler_RejectsSelfDeletion()
    {
        var user = new AppUser { Id = Guid.NewGuid(), IsActive = true };
        var repository = Substitute.For<IUsersRepository>();
        repository.GetTrackedUserAsync(user.Id, false, Arg.Any<CancellationToken>())
            .Returns(user);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        var handler = new DeleteUserHandler(
            repository,
            currentUser,
            Substitute.For<IRefreshTokenService>(),
            Substitute.For<IPermissionService>(),
            Substitute.For<IAuditLogger>());

        var result = await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        Assert.Equal("self_action_forbidden", result.Error.Code);
        await repository.DidNotReceive()
            .UpdateUserAsync(Arg.Any<AppUser>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUserHandler_ProtectsTheLastActiveAdmin()
    {
        var user = new AppUser { Id = Guid.NewGuid(), IsActive = true };
        var repository = Substitute.For<IUsersRepository>();
        repository.GetTrackedUserAsync(user.Id, false, Arg.Any<CancellationToken>())
            .Returns(user);
        repository.GetUserRoleNamesAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(["Admin"]);
        repository.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        var handler = new DeleteUserHandler(
            repository,
            currentUser,
            Substitute.For<IRefreshTokenService>(),
            Substitute.For<IPermissionService>(),
            Substitute.For<IAuditLogger>());

        var result = await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        Assert.Equal("last_admin_protected", result.Error.Code);
        await repository.DidNotReceive()
            .UpdateUserAsync(Arg.Any<AppUser>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>());
    }
}
