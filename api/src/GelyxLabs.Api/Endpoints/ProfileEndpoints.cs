namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Features.Profile;
using GelyxLabs.Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public sealed class ProfileEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapPut("me", UpdateOwnProfileAsync)
            .RequirePermission("profile.update")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> UpdateOwnProfileAsync(
        [FromBody] UpdateOwnProfileRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<UpdateOwnProfileCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new UpdateOwnProfileCommand(request.FullName);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(command, cancellationToken)));
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(
                errors,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "La solicitud contiene errores de validación.");
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        var statusCode = result.Error.Code == "unauthorized"
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status400BadRequest;
        return Results.Problem(
            statusCode: statusCode,
            title: result.Error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });
    }
}

public sealed record UpdateOwnProfileRequest(string FullName);
