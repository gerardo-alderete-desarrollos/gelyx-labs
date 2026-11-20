namespace GelyxLabs.Api.Authorization;

using Microsoft.AspNetCore.Builder;

public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return builder.RequireAuthorization($"{PermissionPolicyProvider.PolicyPrefix}{permission}");
    }
}
