namespace GelyxLabs.Application.Features.Leads;

using GelyxLabs.Application.Abstractions.Cqrs;

public sealed record CreateLeadCommand(
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string? Service,
    string? Budget,
    string Message) : ICommand<LeadDto>;

public sealed record GetLeadsQuery(string? Status = null) : IQuery<IReadOnlyList<LeadDto>>;
public sealed record GetLeadByIdQuery(Guid Id) : IQuery<LeadDto>;
public sealed record UpdateLeadCommand(
    Guid Id,
    string Status,
    string? Notes) : ICommand<LeadDto>;
public sealed record DeleteLeadCommand(Guid Id) : ICommand;
public sealed record RespondLeadCommand(
    Guid Id,
    string Channel,
    string? Subject,
    string? Body) : ICommand<LeadDto>;

public sealed record LeadDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string? Service,
    string? Budget,
    string Message,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ContactedAt,
    DateTimeOffset? RespondedAt,
    string? ResponseChannel);

public interface ILeadsRepository
{
    Task<LeadDto> AddAsync(GelyxLabs.Domain.Entities.Lead lead, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadDto>> GetLeadsAsync(string? status, CancellationToken cancellationToken);
    Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken);
    Task<GelyxLabs.Domain.Entities.Lead?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(GelyxLabs.Domain.Entities.Lead lead, CancellationToken cancellationToken);
}

public static class LeadsError
{
    public static readonly GelyxLabs.Domain.Errors.Error NotFound =
        new("lead_not_found", "Lead was not found.");
}