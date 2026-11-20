namespace GelyxLabs.Application.Features.Projects;

using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;

public sealed class GetProjectsHandler : IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectsRepository _repository;
    public GetProjectsHandler(IProjectsRepository repository) => _repository = repository;
    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(GetProjectsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await _repository.GetProjectsAsync(query.ActiveOnly, cancellationToken));
}

public sealed class GetProjectByIdHandler : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectsRepository _repository;
    public GetProjectByIdHandler(IProjectsRepository repository) => _repository = repository;
    public async Task<Result<ProjectDto>> Handle(GetProjectByIdQuery query, CancellationToken cancellationToken)
    {
        var project = await _repository.GetProjectAsync(query.Id, cancellationToken);
        return project is null
            ? Result.Failure<ProjectDto>(ProjectsError.NotFound)
            : Result.Success(project);
    }
}

public sealed class CreateProjectHandler : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectsRepository _repository;
    private readonly IAuditLogger _auditLogger;
    public CreateProjectHandler(IProjectsRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }
    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = command.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(command.Slug) ? null : command.Slug.Trim(),
            ShortDescription = command.ShortDescription,
            Description = command.Description,
            ImageUrl = command.ImageUrl,
            Url = command.Url,
            Category = command.Category,
            Tags = command.Tags,
            IsFeatured = command.IsFeatured,
            SortOrder = command.SortOrder,
            IsActive = command.IsActive
        };
        await _repository.AddAsync(project, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("project.created", "project", project.Id.ToString(), new Dictionary<string, object?> { ["projectId"] = project.Id, ["name"] = project.Name }, cancellationToken);
        var dto = await _repository.GetProjectAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException("The created project could not be loaded.");
        return Result.Success(dto);
    }
}

public sealed class UpdateProjectHandler : ICommandHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectsRepository _repository;
    private readonly IAuditLogger _auditLogger;
    public UpdateProjectHandler(IProjectsRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }
    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (project is null) return Result.Failure<ProjectDto>(ProjectsError.NotFound);
        project.Name = command.Name.Trim();
        project.Slug = string.IsNullOrWhiteSpace(command.Slug) ? null : command.Slug.Trim();
        project.ShortDescription = command.ShortDescription;
        project.Description = command.Description;
        project.ImageUrl = command.ImageUrl;
        project.Url = command.Url;
        project.Category = command.Category;
        project.Tags = command.Tags;
        project.IsFeatured = command.IsFeatured;
        project.SortOrder = command.SortOrder;
        project.IsActive = command.IsActive;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("project.updated", "project", project.Id.ToString(), new Dictionary<string, object?> { ["projectId"] = project.Id }, cancellationToken);
        var dto = await _repository.GetProjectAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException("The updated project could not be loaded.");
        return Result.Success(dto);
    }
}

public sealed class DeleteProjectHandler : ICommandHandler<DeleteProjectCommand>
{
    private readonly IProjectsRepository _repository;
    private readonly IAuditLogger _auditLogger;
    public DeleteProjectHandler(IProjectsRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }
    public async Task<Result> Handle(DeleteProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (project is null) return Result.Failure(ProjectsError.NotFound);
        await _repository.DeleteAsync(project, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("project.deleted", "project", command.Id.ToString(), new Dictionary<string, object?> { ["projectId"] = command.Id }, cancellationToken);
        return Result.Success();
    }
}