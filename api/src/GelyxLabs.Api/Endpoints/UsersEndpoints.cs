namespace GelyxLabs.Api.Endpoints;

using System.Text;
using GelyxLabs.Api.Authorization;
using GelyxLabs.Api.Common;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Features.Users;
using GelyxLabs.Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public sealed class UsersEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users").WithTags("Users");
        group.MapGet("", GetUsersAsync).RequirePermission("users.read");
        group.MapGet("export", ExportUsersAsync).RequirePermission("users.read");
        group.MapGet("summary", GetSummaryAsync)
            .RequirePermission("users.read")
            .Produces<UsersSummaryDto>(StatusCodes.Status200OK);
        group.MapGet("{id:guid}", GetUserAsync).RequirePermission("users.read");
        group.MapPost("", CreateUserAsync).RequirePermission("users.create");
        group.MapPut("{id:guid}", UpdateUserAsync).RequirePermission("users.update");
        group.MapPatch("{id:guid}/status", SetUserStatusAsync).RequirePermission("users.manage-status");
        group.MapPut("{id:guid}/roles", AssignUserRolesAsync).RequirePermission("users.assign-roles");
        group.MapPost("{id:guid}/reset-password", ResetPasswordAsync).RequirePermission("users.reset-password");
        group.MapDelete("{id:guid}", DeleteUserAsync).RequirePermission("users.delete");
        group.MapPost("{id:guid}/restore", RestoreUserAsync).RequirePermission("users.delete");
    }

    private static async Task<IResult> GetUsersAsync(
        IDispatcher dispatcher,
        IEnumerable<IValidator<GetUsersQuery>> validators,
        int? page,
        int? pageSize,
        string? search,
        Guid? roleId,
        string? status,
        string? sortBy,
        string? sortDir,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(
            Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? 20, 1, 100),
            search,
            roleId,
            status,
            sortBy,
            sortDir);
        var validation = await ValidateAsync(query, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> GetSummaryAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetUsersSummaryQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ExportUsersAsync(
        IUsersRepository usersRepository,
        IAuditLogger auditLogger,
        int? page,
        int? pageSize,
        string? search,
        Guid? roleId,
        string? status,
        string? sortBy,
        string? sortDir,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(
            Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? 20, 1, 100),
            search,
            roleId,
            status,
            sortBy,
            sortDir);
        await auditLogger.LogAsync(
            "export.created",
            "users",
            null,
            new Dictionary<string, object?>
            {
                ["resource"] = "users",
                ["filters"] = new { search, roleId, status, sortBy, sortDir },
                ["maxRows"] = 10_000
            },
            cancellationToken);

        return Results.Stream(async stream =>
        {
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            await CsvExport.WriteRowAsync(
                writer,
                ["Id", "Email", "FullName", "PhoneNumber", "Roles", "IsActive", "IsDeleted", "LastLoginAt"],
                cancellationToken);
            await foreach (var user in usersRepository.StreamUsersAsync(query, cancellationToken))
            {
                await CsvExport.WriteRowAsync(
                    writer,
                    [
                        user.Id.ToString(),
                        user.Email,
                        user.FullName,
                        user.PhoneNumber,
                        string.Join(", ", user.Roles),
                        user.IsActive.ToString(),
                        user.IsDeleted.ToString(),
                        CsvExport.Date(user.LastLoginAt)
                    ],
                    cancellationToken);
            }
        }, "text/csv; charset=utf-8", fileDownloadName: "users.csv");
    }

    private static async Task<IResult> GetUserAsync(
        Guid id,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<CreateUserCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.FullName, request.RoleIds ?? []);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/users/{result.Value.Id}", result.Value)
            : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdateUserAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<UpdateUserCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.Email, request.FullName, request.PhoneNumber, request.RowVersion);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> SetUserStatusAsync(
        Guid id,
        [FromBody] SetUserStatusRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<SetUserStatusCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new SetUserStatusCommand(id, request.IsActive);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> AssignUserRolesAsync(
        Guid id,
        [FromBody] AssignUserRolesRequest request,
        IDispatcher dispatcher,
        IEnumerable<IValidator<AssignUserRolesCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new AssignUserRolesCommand(id, request.RoleIds ?? []);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> ResetPasswordAsync(
        Guid id,
        IDispatcher dispatcher,
        IEnumerable<IValidator<ResetUserPasswordCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new ResetUserPasswordCommand(id);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { temporaryPassword = result.Value })
            : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> DeleteUserAsync(
        Guid id,
        IDispatcher dispatcher,
        IEnumerable<IValidator<DeleteUserCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand(id);
        var validation = await ValidateAsync(command, validators, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }
        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> RestoreUserAsync(
        Guid id,
        IDispatcher dispatcher,
        IEnumerable<IValidator<RestoreUserCommand>> validators,
        CancellationToken cancellationToken)
    {
        var command = new RestoreUserCommand(id);
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
            "user_not_found" => StatusCodes.Status404NotFound,
            "self_action_forbidden" => StatusCodes.Status403Forbidden,
            "last_admin_protected" or "concurrency_conflict" => StatusCodes.Status409Conflict,
            "email_taken" or "invalid_roles" => StatusCodes.Status422UnprocessableEntity,
            "user_not_deleted" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        var fieldErrors = error.Code switch
        {
            "email_taken" => new Dictionary<string, string[]> { ["email"] = [error.Description] },
            "invalid_roles" => new Dictionary<string, string[]> { ["roleIds"] = [error.Description] },
            _ => null
        };
        if (fieldErrors is not null)
        {
            extensions["errors"] = fieldErrors;
        }
        var problem = Results.Problem(statusCode: statusCode, title: error.Description, extensions: extensions);
        return problem;
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

public sealed record CreateUserRequest(string Email, string FullName, Guid[]? RoleIds);
public sealed record UpdateUserRequest(string Email, string FullName, string? PhoneNumber, uint RowVersion);
public sealed record SetUserStatusRequest(bool IsActive);
public sealed record AssignUserRolesRequest(Guid[]? RoleIds);
