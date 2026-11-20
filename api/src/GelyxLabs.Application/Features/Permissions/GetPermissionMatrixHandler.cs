namespace GelyxLabs.Application.Features.Permissions;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed class GetPermissionMatrixHandler : IQueryHandler<GetPermissionMatrixQuery, PermissionMatrixDto>
{
    private readonly IPermissionsRepository _repository;

    public GetPermissionMatrixHandler(IPermissionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PermissionMatrixDto>> Handle(
        GetPermissionMatrixQuery query,
        CancellationToken cancellationToken)
    {
        return Result.Success(await _repository.GetMatrixAsync(cancellationToken));
    }
}
