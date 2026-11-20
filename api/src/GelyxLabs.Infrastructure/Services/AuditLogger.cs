using System.Text.Json;
using System.Text.Json.Nodes;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace GelyxLabs.Infrastructure.Services;

public sealed class AuditLogger : IAuditLogger
{
    private static readonly string[] SensitiveKeyParts = ["password", "token", "hash", "secret"];
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogger(
        AppDbContext context,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        IReadOnlyDictionary<string, object?> metadata,
        CancellationToken cancellationToken = default)
    {
        var metadataNode = JsonSerializer.SerializeToNode(metadata) as JsonObject ?? new JsonObject();
        Sanitize(metadataNode);
        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = _currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Metadata = metadataNode.ToJsonString(),
            Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void Sanitize(JsonObject value)
    {
        foreach (var key in value.Select(property => property.Key).ToArray())
        {
            if (SensitiveKeyParts.Any(part => key.Contains(part, StringComparison.OrdinalIgnoreCase)))
            {
                value.Remove(key);
                continue;
            }

            SanitizeNode(value[key]);
        }
    }

    private static void SanitizeNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject nestedObject:
                Sanitize(nestedObject);
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    SanitizeNode(item);
                }
                break;
        }
    }
}
