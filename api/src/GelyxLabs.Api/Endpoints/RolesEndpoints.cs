namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Features.Roles;
using GelyxLabs.Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public sealed class RolesEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("roles").WithTags("Roles");
        group.MapGet("", GetRolesAsync).RequirePermission("roles.read");
        group.MapGet("{id:guid}", GetRoleAsync).RequirePermission("roles.read");
        group.MapPost("", CreateRoleAsync).RequirePermission("roles.create");
        group.MapPut("{id:guid}", UpdateRoleAsync).RequirePermission("roles.update");
        group.MapDelete("{id:guid}", DeleteRoleAsync).RequirePermission("roles.delete");
    }

    private static async Task<IResult> GetRolesAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetRolesQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetRoleAsync(
        Guid id,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetRoleByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromBody] CreateRoleRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<CreateRoleCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Name, request.Description, request.CopyPermissionsFromRoleId);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/roles/{result.Value.Id}", result.Value)
            : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<UpdateRoleCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(id, request.Name, request.Description);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid id,
        Guid? reassignToRoleId,
        IDispatcher dispatcher,
        IEnumerable<IValidator<DeleteRoleCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoleCommand(id, reassignToRoleId);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static IResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "role_not_found" or "copy_source_not_found" => StatusCodes.Status404NotFound,
            "system_role_protected" or "role_has_users" or "invalid_reassignment" => StatusCodes.Status409Conflict,
            "role_name_invalid" or "role_name_taken" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        var fieldErrors = error.Code switch
        {
            "role_name_invalid" or "role_name_taken" => new Dictionary<string, string[]> { ["name"] = [error.Description] },
            "copy_source_not_found" => new Dictionary<string, string[]> { ["copyPermissionsFromRoleId"] = [error.Description] },
            _ => null
        };
        if (fieldErrors is not null)
        {
            extensions["errors"] = fieldErrors;
        }
        return Results.Problem(statusCode: statusCode, title: error.Description, extensions: extensions);
    }

    private static async Task<IResult?> ValidateAsync<T>(
        T value,
        IEnumerable<IValidator<T>> validators,
        CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(value, cancellationToken)));
        var errors = results.SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
        return errors.Count == 0
            ? null
            : Results.ValidationProblem(
                errors,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "La solicitud contiene errores de validación.");
    }
}

public sealed record CreateRoleRequest(string Name, string? Description, Guid? CopyPermissionsFromRoleId);
public sealed record UpdateRoleRequest(string Name, string? Description);
