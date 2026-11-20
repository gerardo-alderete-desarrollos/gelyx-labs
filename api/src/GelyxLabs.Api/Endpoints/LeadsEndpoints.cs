namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Features.Leads;
using GelyxLabs.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

public sealed class LeadsEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("leads").WithTags("Leads");
        group.MapPost("", CreateAsync).AllowAnonymous();
        group.MapGet("", GetAllAsync).RequirePermission("leads.read");
        group.MapGet("{id:guid}", GetByIdAsync).RequirePermission("leads.read");
        group.MapPut("{id:guid}", UpdateAsync).RequirePermission("leads.update");
        group.MapDelete("{id:guid}", DeleteAsync).RequirePermission("leads.delete");
        group.MapPost("{id:guid}/respond", RespondAsync).RequirePermission("leads.respond");
    }

    private static async Task<IResult> CreateAsync([FromBody] CreateLeadRequest request, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var command = new CreateLeadCommand(request.Name, request.Email, request.Phone, request.Company, request.Service, request.Budget, request.Message);
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Created($"/api/v1/leads/{result.Value!.Id}", result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> GetAllAsync(IDispatcher dispatcher, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetLeadsQuery(status), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetLeadByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdateAsync(Guid id, [FromBody] UpdateLeadRequest request, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var command = new UpdateLeadCommand(id, request.Status, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> DeleteAsync(Guid id, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteLeadCommand(id), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> RespondAsync(Guid id, [FromBody] RespondLeadRequest request, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var command = new RespondLeadCommand(id, request.Channel, request.Subject, request.Body);
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static IResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "lead_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(statusCode: statusCode, title: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

public sealed record CreateLeadRequest(
    string Name, string Email, string? Phone, string? Company, string? Service, string? Budget, string Message);
public sealed record UpdateLeadRequest(string Status, string? Notes);
public sealed record RespondLeadRequest(string Channel, string? Subject, string? Body);