namespace GelyxLabs.Application.Abstractions.Security;

public interface IPermissionService
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    void InvalidateUser(Guid userId);

    void InvalidateAll();
}
