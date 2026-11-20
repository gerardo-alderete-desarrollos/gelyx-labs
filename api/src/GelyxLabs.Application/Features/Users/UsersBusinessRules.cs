namespace GelyxLabs.Application.Features.Users;

public static class UsersBusinessRules
{
    public static bool IsSelfAction(Guid? actorUserId, Guid targetUserId) =>
        actorUserId == targetUserId;

    public static bool WouldRemoveLastActiveAdmin(bool isActive, bool hasAdminRole, int activeAdminCount) =>
        isActive && hasAdminRole && activeAdminCount <= 1;
}
