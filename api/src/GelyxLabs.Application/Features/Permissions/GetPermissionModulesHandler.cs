namespace GelyxLabs.Application.Features.Permissions;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed class GetPermissionModulesHandler : IQueryHandler<GetPermissionModulesQuery, IReadOnlyList<PermissionModuleDto>>
{
    private readonly IPermissionsRepository _repository;

    public GetPermissionModulesHandler(IPermissionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<PermissionModuleDto>>> Handle(
        GetPermissionModulesQuery query,
        CancellationToken cancellationToken)
    {
        return Result.Success(await _repository.GetModulesAsync(cancellationToken));
    }
}
