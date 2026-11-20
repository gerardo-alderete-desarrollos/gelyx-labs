namespace GelyxLabs.Infrastructure.Services;

using GelyxLabs.Application.Features.Projects;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ProjectsRepository : IProjectsRepository
{
    private readonly AppDbContext _context;

    public ProjectsRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsNoTracking();
        if (activeOnly) query = query.Where(p => p.IsActive);
        var items = await query.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<ProjectDto?> GetProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await _context.Projects.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return project is null ? null : Map(project);
    }

    public Task<Project?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Projects.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
        return project;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        _context.Projects.Remove(project);
        return Task.CompletedTask;
    }

    private static ProjectDto Map(Project p) => new(
        p.Id, p.Name, p.Slug, p.ShortDescription, p.Description, p.ImageUrl, p.Url,
        p.Category, p.Tags, p.IsFeatured, p.SortOrder, p.IsActive, p.CreatedAt, p.UpdatedAt);
}