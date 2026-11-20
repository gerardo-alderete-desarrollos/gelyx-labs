namespace GelyxLabs.Application.Features.Projects;

using GelyxLabs.Application.Abstractions.Cqrs;

public sealed record GetProjectsQuery(bool ActiveOnly = true) : IQuery<IReadOnlyList<ProjectDto>>;
public sealed record GetProjectByIdQuery(Guid Id) : IQuery<ProjectDto>;
public sealed record CreateProjectCommand(
    string Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? ImageUrl,
    string? Url,
    string? Category,
    string? Tags,
    bool IsFeatured,
    int SortOrder,
    bool IsActive) : ICommand<ProjectDto>;
public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? ImageUrl,
    string? Url,
    string? Category,
    string? Tags,
    bool IsFeatured,
    int SortOrder,
    bool IsActive) : ICommand<ProjectDto>;
public sealed record DeleteProjectCommand(Guid Id) : ICommand;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? ImageUrl,
    string? Url,
    string? Category,
    string? Tags,
    bool IsFeatured,
    int SortOrder,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public interface IProjectsRepository
{
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<ProjectDto?> GetProjectAsync(Guid id, CancellationToken cancellationToken);
    Task<GelyxLabs.Domain.Entities.Project?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GelyxLabs.Domain.Entities.Project> AddAsync(GelyxLabs.Domain.Entities.Project project, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(GelyxLabs.Domain.Entities.Project project, CancellationToken cancellationToken);
}

public static class ProjectsError
{
    public static readonly GelyxLabs.Domain.Errors.Error NotFound =
        new("project_not_found", "Project was not found.");
    public static readonly GelyxLabs.Domain.Errors.Error SlugTaken =
        new("project_slug_taken", "A project with this slug already exists.");
}