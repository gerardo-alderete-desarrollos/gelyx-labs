namespace GelyxLabs.Application.Features.Audit;

using GelyxLabs.Application.Abstractions.Cqrs;

public sealed class GetAuditLogsHandler : IQueryHandler<GetAuditLogsQuery, AuditLogsPageDto>
{
    private readonly IAuditLogRepository _repository;

    public GetAuditLogsHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<GelyxLabs.Domain.Common.Result<AuditLogsPageDto>> Handle(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken) =>
        GelyxLabs.Domain.Common.Result.Success(await _repository.GetAsync(query, cancellationToken));
}
