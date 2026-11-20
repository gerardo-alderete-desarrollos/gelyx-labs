namespace GelyxLabs.Api.Endpoints;

using System.Text;
using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Features.Audit;

public sealed class AuditLogsEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("audit-logs", GetAuditLogsAsync)
            .WithTags("Audit")
            .RequirePermission("audit.read");
        app.MapGet("audit-logs/export", ExportAuditLogsAsync)
            .WithTags("Audit")
            .RequirePermission("audit.read");
    }

    private static async Task<IResult> GetAuditLogsAsync(
        IDispatcher dispatcher,
        int? page,
        int? pageSize,
        string? entityType,
        string? action,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(page ?? 1, 1);
        var normalizedPageSize = Math.Clamp(pageSize ?? 20, 1, 100);
        var result = await dispatcher.Send(
            new GetAuditLogsQuery(normalizedPage, normalizedPageSize, entityType, action, actorUserId),
            cancellationToken);
        return Results.Ok(new
        {
            items = result.Value.Items,
            page = result.Value.Page,
            pageSize = result.Value.PageSize,
            totalCount = result.Value.TotalCount,
            totalPages = result.Value.TotalPages
        });
    }

    private static async Task<IResult> ExportAuditLogsAsync(
        IAuditLogRepository auditLogsRepository,
        IAuditLogger auditLogger,
        int? page,
        int? pageSize,
        string? entityType,
        string? action,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var query = new GetAuditLogsQuery(
            Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? 20, 1, 100),
            entityType,
            action,
            actorUserId);
        await auditLogger.LogAsync(
            "export.created",
            "audit-logs",
            null,
            new Dictionary<string, object?>
            {
                ["resource"] = "audit-logs",
                ["filters"] = new { entityType, action, actorUserId },
                ["maxRows"] = 10_000
            },
            cancellationToken);

        return Results.Stream(async stream =>
        {
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            await CsvExport.WriteRowAsync(
                writer,
                ["Id", "ActorUserId", "Action", "EntityType", "EntityId", "Metadata", "Ip", "CreatedAt"],
                cancellationToken);
            await foreach (var log in auditLogsRepository.StreamAsync(query, cancellationToken))
            {
                await CsvExport.WriteRowAsync(
                    writer,
                    [
                        log.Id.ToString(),
                        log.ActorUserId?.ToString(),
                        log.Action,
                        log.EntityType,
                        log.EntityId,
                        log.Metadata.GetRawText(),
                        log.Ip,
                        CsvExport.Date(log.CreatedAt)
                    ],
                    cancellationToken);
            }
        }, "text/csv; charset=utf-8", fileDownloadName: "audit-logs.csv");
    }
}
