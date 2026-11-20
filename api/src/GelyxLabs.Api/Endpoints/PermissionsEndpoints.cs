namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Features.Permissions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public sealed class PermissionsEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("permissions")
            .WithTags("Permissions");

        group.MapGet("modules", GetModulesAsync)
            .RequirePermission("permissions.read");
        group.MapGet("matrix", GetMatrixAsync)
            .RequirePermission("permissions.read");
        group.MapGet("roles/{roleId:guid}", GetRoleAsync)
            .RequirePermission("permissions.read");
        group.MapPut("roles/{roleId:guid}", UpdateRoleAsync)
            .RequirePermission("permissions.manage");
    }

    private static async Task<IResult> GetModulesAsync(
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPermissionModulesQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetMatrixAsync(
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPermissionMatrixQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetRoleAsync(
        Guid roleId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetRolePermissionsQuery(roleId), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        IEnumerable<IValidator<UpdateRolePermissionsCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRolePermissionsCommand(roleId, request.Permissions ?? []);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(command, cancellationToken)));
        var validationErrors = validationResults
            .SelectMany(validation => validation.Errors)
            .Where(error => error is not null)
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors,
                statusCode: StatusCodes.Status400BadRequest,
                title: "La solicitud contiene errores de validación.");
        }

        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static IResult CreateProblemResult(GelyxLabs.Domain.Errors.Error error)
    {
        var statusCode = error.Code switch
        {
            "role_not_found" => StatusCodes.Status404NotFound,
            "admin_role_protected" => StatusCodes.Status409Conflict,
            "system_permissions_restricted" or "permission_not_found" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

public sealed record UpdateRolePermissionsRequest(string[]? Permissions);
