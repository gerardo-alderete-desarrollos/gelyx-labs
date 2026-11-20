using System.Text.Json;
using GelyxLabs.Application.Features.Audit;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace GelyxLabs.Infrastructure.Services;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogsPageDto> GetAsync(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var logs = BuildLogsQuery(query);

        var totalCount = await logs.CountAsync(cancellationToken);
        var offset = (int)Math.Min((long)(query.Page - 1) * query.PageSize, int.MaxValue);
        var pageItems = await logs
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(log => new
            {
                log.Id,
                log.ActorUserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Metadata,
                log.Ip,
                log.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var items = pageItems.Select(log =>
        {
            using var metadata = JsonDocument.Parse(log.Metadata);
            return new AuditLogDto(
                log.Id,
                log.ActorUserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                metadata.RootElement.Clone(),
                log.Ip,
                log.CreatedAt);
        }).ToArray();

        return new AuditLogsPageDto(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    public async IAsyncEnumerable<AuditLogDto> StreamAsync(
        GetAuditLogsQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var logs = BuildLogsQuery(query)
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Take(10_000)
            .Select(log => new
            {
                log.Id,
                log.ActorUserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Metadata,
                log.Ip,
                log.CreatedAt
            });

        await foreach (var log in logs.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            using var metadata = JsonDocument.Parse(log.Metadata);
            yield return new AuditLogDto(
                log.Id,
                log.ActorUserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                metadata.RootElement.Clone(),
                log.Ip,
                log.CreatedAt);
        }
    }

    private IQueryable<AuditLog> BuildLogsQuery(GetAuditLogsQuery query)
    {
        var logs = _context.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logs = logs.Where(log => log.EntityType == query.EntityType);
        }
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(log => log.Action == query.Action);
        }
        if (query.ActorUserId is { } actorUserId)
        {
            logs = logs.Where(log => log.ActorUserId == actorUserId);
        }
        return logs;
    }
}
