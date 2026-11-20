namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Features.Projects;
using GelyxLabs.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

public sealed class ProjectsEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("projects").WithTags("Projects");
        group.MapGet("", GetAllAsync).AllowAnonymous();
        group.MapGet("{id:guid}", GetByIdAsync).AllowAnonymous();
        group.MapPost("", CreateAsync).RequirePermission("projects.create");
        group.MapPut("{id:guid}", UpdateAsync).RequirePermission("projects.update");
        group.MapDelete("{id:guid}", DeleteAsync).RequirePermission("projects.delete");
    }

    private static async Task<IResult> GetAllAsync(
        IDispatcher dispatcher,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetProjectsQuery(activeOnly ?? true), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetProjectByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> CreateAsync([FromBody] CreateProjectRequest request, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(
            request.Name, request.Slug, request.ShortDescription, request.Description,
            request.ImageUrl, request.Url, request.Category, request.Tags,
            request.IsFeatured, request.SortOrder, request.IsActive);
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Created($"/api/v1/projects/{result.Value!.Id}", result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdateAsync(Guid id, [FromBody] UpdateProjectRequest request, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand(
            id, request.Name, request.Slug, request.ShortDescription, request.Description,
            request.ImageUrl, request.Url, request.Category, request.Tags,
            request.IsFeatured, request.SortOrder, request.IsActive);
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> DeleteAsync(Guid id, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteProjectCommand(id), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static IResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "project_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(statusCode: statusCode, title: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

public sealed record CreateProjectRequest(
    string Name, string? Slug, string? ShortDescription, string? Description,
    string? ImageUrl, string? Url, string? Category, string? Tags,
    bool IsFeatured, int SortOrder, bool IsActive);
public sealed record UpdateProjectRequest(
    string Name, string? Slug, string? ShortDescription, string? Description,
    string? ImageUrl, string? Url, string? Category, string? Tags,
    bool IsFeatured, int SortOrder, bool IsActive);