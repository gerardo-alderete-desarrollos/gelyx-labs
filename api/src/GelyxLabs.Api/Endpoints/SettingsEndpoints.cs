namespace GelyxLabs.Api.Endpoints;

using System.Text.Json;
using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Abstractions.Settings;
using GelyxLabs.Application.Features.Settings;
using GelyxLabs.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

public sealed class SettingsEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("settings").WithTags("Settings");
        group.MapGet("", GetSettingsAsync).RequirePermission("settings.access").RequirePermission("settings.read");
        group.MapPut("{key}", UpdateSettingAsync).RequirePermission("settings.access").RequirePermission("settings.update");
        group.MapGet("public", GetPublicSettingsAsync).AllowAnonymous();
    }

    private static async Task<IResult> GetSettingsAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetSettingsQuery(), cancellationToken);
        return Results.Ok(result.Value.Select(ToResponse));
    }

    private static async Task<IResult> GetPublicSettingsAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPublicSettingsQuery(), cancellationToken);
        return Results.Ok(result.Value.Select(ToResponse));
    }

    private static async Task<IResult> UpdateSettingAsync(
        string key,
        [FromBody] UpdateSettingRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UpdateSettingCommand(key, request.Value.GetRawText()), cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value)) : CreateProblemResult(result.Error);
    }

    private static SettingResponse ToResponse(AppSettingValue setting)
    {
        using var document = JsonDocument.Parse(setting.Value);
        return new SettingResponse(setting.Key, document.RootElement.Clone(), setting.Type, setting.UpdatedAt, setting.UpdatedBy);
    }

    private static IResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "setting_not_found" => StatusCodes.Status404NotFound,
            "setting_type_invalid" or "setting_value_invalid" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(statusCode: statusCode, title: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

public sealed record UpdateSettingRequest(JsonElement Value);
public sealed record SettingResponse(string Key, JsonElement Value, string Type, DateTimeOffset UpdatedAt, Guid? UpdatedBy);
