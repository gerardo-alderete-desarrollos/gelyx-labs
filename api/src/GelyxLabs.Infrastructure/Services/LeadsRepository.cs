namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Features.Leads;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class LeadsRepository : ILeadsRepository
{
    private readonly AppDbContext _context;

    public LeadsRepository(AppDbContext context) => _context = context;

    public async Task<LeadDto> AddAsync(Lead lead, CancellationToken cancellationToken)
    {
        await _context.Leads.AddAsync(lead, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(lead);
    }

    public async Task<IReadOnlyList<LeadDto>> GetLeadsAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _context.Leads.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
        var items = await query.OrderByDescending(l => l.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken)
    {
        var lead = await _context.Leads.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        return lead is null ? null : Map(lead);
    }

    public Task<Lead?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Leads.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task DeleteAsync(Lead lead, CancellationToken cancellationToken)
    {
        _context.Leads.Remove(lead);
        return Task.CompletedTask;
    }

    private static LeadDto Map(Lead l) => new(
        l.Id, l.Name, l.Email, l.Phone, l.Company, l.Service, l.Budget, l.Message,
        l.Status, l.Notes, l.CreatedAt, l.ContactedAt, l.RespondedAt, l.ResponseChannel);
}