namespace GelyxLabs.Application.Features.Permissions;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed class GetRolePermissionsHandler : IQueryHandler<GetRolePermissionsQuery, RolePermissionsDto>
{
    private readonly IPermissionsRepository _repository;

    public GetRolePermissionsHandler(IPermissionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RolePermissionsDto>> Handle(
        GetRolePermissionsQuery query,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetRolePermissionsAsync(query.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RolePermissionsDto>(
                new GelyxLabs.Domain.Errors.Error("role_not_found", $"Role '{query.RoleId}' was not found."));
        }
        return Result.Success(role);
    }
}
