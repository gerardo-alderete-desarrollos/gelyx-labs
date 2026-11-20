namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Features.Modules;
using GelyxLabs.Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public sealed class ModulesEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("modules").WithTags("Modules");
        group.MapGet("", GetModulesAsync).RequirePermission("modules.read");
        group.MapGet("{id:guid}", GetModuleAsync).RequirePermission("modules.read");
        group.MapPut("{id:guid}", UpdateModuleAsync).RequirePermission("modules.update");
        group.MapPatch("{id:guid}/status", SetModuleStatusAsync).RequirePermission("modules.update");
    }

    private static async Task<IResult> GetModulesAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetModulesQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetModuleAsync(
        Guid id,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetModuleByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdateModuleAsync(
        Guid id,
        [FromBody] UpdateModuleRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<UpdateModuleCommand>> validators,
        CancellationToken cancellationToken)
    {
        var translations = (request.Translations ?? new Dictionary<string, ModuleTranslationRequest>())
            .ToDictionary(
                translation => translation.Key,
                translation => new ModuleTranslationInput(
                    translation.Value?.Name ?? string.Empty,
                    translation.Value?.Description),
                StringComparer.Ordinal);
        var command = new UpdateModuleCommand(id, translations, request.Icon, request.SortOrder);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> SetModuleStatusAsync(
        Guid id,
        [FromBody] SetModuleStatusRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new SetModuleStatusCommand(id, request.IsActive), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static IResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "module_not_found" => StatusCodes.Status404NotFound,
            "module_status_protected" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(
            statusCode: statusCode,
            title: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
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

public sealed record ModuleTranslationRequest(string Name, string? Description);
public sealed record UpdateModuleRequest(
    Dictionary<string, ModuleTranslationRequest>? Translations,
    string? Icon,
    int SortOrder);
public sealed record SetModuleStatusRequest(bool IsActive);
