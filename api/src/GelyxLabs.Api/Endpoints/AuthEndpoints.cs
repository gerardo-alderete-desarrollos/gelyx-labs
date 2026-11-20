namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Dispatching;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Application.Features.Auth.Login;
using GelyxLabs.Application.Features.Auth.Refresh;
using GelyxLabs.Application.Features.Auth.ForgotPassword;
using GelyxLabs.Application.Features.Auth.Logout;
using GelyxLabs.Application.Features.Auth.Me;
using GelyxLabs.Application.Features.Auth.Register;
using GelyxLabs.Application.Features.Auth.ChangePassword;
using GelyxLabs.Application.Features.Auth.ResetPassword;
using GelyxLabs.Application.Features.Auth.Sessions;
using GelyxLabs.Api.Common;
using GelyxLabs.Api.Authorization;
using GelyxLabs.Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;

public class AuthEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("auth")
            .WithTags("Auth")
            .DisableAntiforgery();

        group.MapPost("register", RegisterAsync)
            .RequireRateLimiting("auth")
            .WithName("Register")
            .Produces<GelyxLabs.Application.Features.Auth.Register.RegisterResponse>(StatusCodes.Status201Created);

        group.MapPost("login", LoginAsync)
            .RequireRateLimiting("auth")
            .WithName("Login")
            .Produces<GelyxLabs.Application.Features.Auth.Login.AuthResponse>(StatusCodes.Status200OK);

        group.MapGet("2fa", GetTwoFactorStatusAsync)
            .RequireAuthorization().WithName("GetTwoFactorStatus");
        group.MapPost("2fa/setup", SetupTwoFactorAsync)
            .RequireAuthorization().RequireRateLimiting("auth").WithName("SetupTwoFactor");
        group.MapPost("2fa/enable", EnableTwoFactorAsync)
            .RequireAuthorization().RequireRateLimiting("auth").WithName("EnableTwoFactor");
        group.MapPost("2fa/disable", DisableTwoFactorAsync)
            .RequireAuthorization().RequireRateLimiting("auth").WithName("DisableTwoFactor");
        group.MapPost("login/2fa", LoginTwoFactorAsync)
            .RequireRateLimiting("auth").AllowAnonymous().WithName("LoginTwoFactor");

        group.MapPost("refresh", RefreshAsync)
            .RequireRateLimiting("refresh")
            .WithName("Refresh")
            .Produces<GelyxLabs.Application.Features.Auth.Refresh.AuthResponse>(StatusCodes.Status200OK);

        group.MapPost("logout", LogoutAsync)
            .RequireRateLimiting("refresh")
            .AllowAnonymous()
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("me", MeAsync)
            .RequireAuthorization()
            .WithName("Me")
            .Produces<GelyxLabs.Application.Features.Auth.Me.MeResponse>(StatusCodes.Status200OK);

        group.MapPut("me/preferences", UpdatePreferencesAsync)
            .RequireAuthorization()
            .WithName("UpdatePreferences")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("sessions", GetSessionsAsync)
            .RequirePermission("sessions.access")
            .RequirePermission("sessions.read")
            .WithName("GetSessions")
            .Produces<IReadOnlyList<SessionDto>>(StatusCodes.Status200OK);

        group.MapDelete("sessions/{id:guid}", RevokeSessionAsync)
            .RequirePermission("sessions.access")
            .RequirePermission("sessions.update")
            .WithName("RevokeSession")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("sessions/revoke-others", RevokeOtherSessionsAsync)
            .RequirePermission("sessions.access")
            .RequirePermission("sessions.update")
            .WithName("RevokeOtherSessions")
            .Produces<RevokeOtherSessionsResponse>(StatusCodes.Status200OK);

        group.MapPost("change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .RequireRateLimiting("auth")
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("forgot-password", ForgotPasswordAsync)
            .RequireRateLimiting("auth")
            .AllowAnonymous()
            .WithName("ForgotPassword")
            .Produces(StatusCodes.Status202Accepted);

        group.MapPost("reset-password", ResetPasswordAsync)
            .RequireRateLimiting("auth")
            .AllowAnonymous()
            .WithName("ResetPassword")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.Register.RegisterCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.Register.RegisterCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.Created($"/api/v1/auth/register", result.Value);
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.Login.LoginCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.Login.LoginCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> GetTwoFactorStatusAsync(UserManager<AppUser> users, ICurrentUserService current)
    {
        var user = current.UserId is { } userId ? await users.FindByIdAsync(userId.ToString()) : null;
        return user is null ? Results.Unauthorized() : Results.Ok(new { enabled = await users.GetTwoFactorEnabledAsync(user) });
    }

    private static async Task<IResult> SetupTwoFactorAsync(UserManager<AppUser> users, ICurrentUserService current, IAuditLogger audit, CancellationToken cancellationToken)
    {
        var user = current.UserId is { } userId ? await users.FindByIdAsync(userId.ToString()) : null;
        if (user is null) return Results.Unauthorized();
        if (await users.GetTwoFactorEnabledAsync(user))
        {
            await audit.LogAsync("2fa.setup_failed", "User", user.Id.ToString(), new Dictionary<string, object?> { ["reason"] = "already_enabled" }, cancellationToken);
            return TwoFactorProblem("two_factor_already_enabled", "La autenticación de dos factores ya está activa.");
        }
        await users.ResetAuthenticatorKeyAsync(user);
        var secret = await users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(secret)) return TwoFactorProblem("two_factor_setup_failed", "No se pudo configurar la autenticación de dos factores.");
        var issuer = Uri.EscapeDataString("GelyxLabs");
        var account = Uri.EscapeDataString(user.Email ?? user.Id.ToString());
        await audit.LogAsync("2fa.setup", "User", user.Id.ToString(), new Dictionary<string, object?>(), cancellationToken);
        return Results.Ok(new { secret, otpauthUri = $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits=6" });
    }

    private static async Task<IResult> EnableTwoFactorAsync(TwoFactorCodeRequest request, UserManager<AppUser> users, ICurrentUserService current, AppDbContext db, IAuditLogger audit, CancellationToken cancellationToken)
    {
        var user = current.UserId is { } userId ? await users.FindByIdAsync(userId.ToString()) : null;
        if (user is null) return Results.Unauthorized();
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, request.Code))
        {
            await audit.LogAsync("2fa.enable_failed", "User", user.Id.ToString(), new Dictionary<string, object?> { ["reason"] = "invalid_code" }, cancellationToken);
            return TwoFactorProblem("invalid_two_factor_code", "El código de autenticación no es válido.");
        }
        await users.SetTwoFactorEnabledAsync(user, true);
        db.TwoFactorRecoveryCodes.RemoveRange(db.TwoFactorRecoveryCodes.Where(code => code.UserId == user.Id));
        var hasher = new PasswordHasher<AppUser>();
        var codes = Enumerable.Range(0, 10).Select(_ => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(9))).ToArray();
        db.TwoFactorRecoveryCodes.AddRange(codes.Select(code => new TwoFactorRecoveryCode { Id = Guid.NewGuid(), UserId = user.Id, CodeHash = hasher.HashPassword(user, code), CreatedAt = DateTimeOffset.UtcNow }));
        await db.SaveChangesAsync(cancellationToken);
        await audit.LogAsync("2fa.enabled", "User", user.Id.ToString(), new Dictionary<string, object?>(), cancellationToken);
        return Results.Ok(new { recoveryCodes = codes });
    }

    private static async Task<IResult> DisableTwoFactorAsync(DisableTwoFactorRequest request, UserManager<AppUser> users, ICurrentUserService current, AppDbContext db, IAuditLogger audit, CancellationToken cancellationToken)
    {
        var user = current.UserId is { } userId ? await users.FindByIdAsync(userId.ToString()) : null;
        if (user is null) return Results.Unauthorized();
        var password = await users.CheckPasswordAsync(user, request.CurrentPassword);
        var codeValid = password && await VerifyCodeAsync(user, request.Code, users, db, cancellationToken);
        if (!password || !codeValid)
        {
            await audit.LogAsync("2fa.disable_failed", "User", user.Id.ToString(), new Dictionary<string, object?> { ["reason"] = !password ? "invalid_password" : "invalid_code" }, cancellationToken);
            return TwoFactorProblem("two_factor_disable_failed", "No se pudo desactivar la autenticación de dos factores.");
        }
        await users.SetTwoFactorEnabledAsync(user, false);
        db.TwoFactorRecoveryCodes.RemoveRange(db.TwoFactorRecoveryCodes.Where(item => item.UserId == user.Id));
        await db.SaveChangesAsync(cancellationToken);
        await audit.LogAsync("2fa.disabled", "User", user.Id.ToString(), new Dictionary<string, object?>(), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> LoginTwoFactorAsync(LoginTwoFactorRequest request, UserManager<AppUser> users, ITokenService tokens, IRefreshTokenService refresh, IMemoryCache cache, AppDbContext db, IAuditLogger audit, CancellationToken cancellationToken)
    {
        var userId = tokens.ValidateTwoFactorToken(request.TwoFactorToken);
        if (userId is null || !cache.TryGetValue($"two-factor:{request.TwoFactorToken}", out _))
        {
            await audit.LogAsync("2fa.login_failed", "User", null, new Dictionary<string, object?> { ["reason"] = "invalid_temporary_token" }, cancellationToken);
            return TwoFactorProblem("invalid_two_factor_token", "El desafío de autenticación ya no es válido.");
        }
        var user = await users.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.IsActive || user.IsDeleted || !await users.GetTwoFactorEnabledAsync(user) || !await VerifyCodeAsync(user, request.Code, users, db, cancellationToken))
        {
            await audit.LogAsync("2fa.login_failed", "User", userId.Value.ToString(), new Dictionary<string, object?> { ["reason"] = "invalid_code" }, cancellationToken);
            return TwoFactorProblem("invalid_two_factor_code", "El código de autenticación no es válido.");
        }
        cache.Remove($"two-factor:{request.TwoFactorToken}");
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await users.UpdateAsync(user);
        var roles = await users.GetRolesAsync(user);
        var refreshToken = await refresh.CreateAsync(user.Id, null, cancellationToken);
        return Results.Ok(new GelyxLabs.Application.Features.Auth.Login.AuthResponse(tokens.GenerateAccessToken(user.Id, user.Email ?? string.Empty, roles), refreshToken.RefreshToken, 15 * 60));
    }

    private static async Task<bool> VerifyCodeAsync(AppUser user, string code, UserManager<AppUser> users, AppDbContext db, CancellationToken cancellationToken)
    {
        if (await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code)) return true;
        var recoveryCodes = await db.TwoFactorRecoveryCodes.Where(item => item.UserId == user.Id && item.UsedAt == null).ToListAsync(cancellationToken);
        var hasher = new PasswordHasher<AppUser>();
        var recoveryCode = recoveryCodes.FirstOrDefault(item => hasher.VerifyHashedPassword(user, item.CodeHash, code) != PasswordVerificationResult.Failed);
        if (recoveryCode is null) return false;
        var consumed = await db.TwoFactorRecoveryCodes
            .Where(item => item.Id == recoveryCode.Id && item.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.UsedAt, DateTimeOffset.UtcNow),
                cancellationToken);
        return consumed == 1;
    }

    private static IResult TwoFactorProblem(string code, string title) => Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });

    private static async Task<IResult> RefreshAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.Refresh.RefreshCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.Refresh.RefreshCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.Logout.LogoutCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.Logout.LogoutCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> MeAsync(
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GelyxLabs.Application.Features.Auth.Me.MeQuery(), cancellationToken);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> UpdatePreferencesAsync(
        [FromBody] UpdateUserPreferencesRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new UpdateUserPreferencesCommand(request.Locale, request.Theme),
            cancellationToken);
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        var statusCode = result.Error.Code switch
        {
            "unauthorized" => StatusCodes.Status401Unauthorized,
            "unsupported_locale" or "unsupported_theme" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(
            statusCode: statusCode,
            title: result.Error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });
    }

    private static async Task<IResult> GetSessionsAsync(
        HttpContext httpContext,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetSessionsQuery(GetCurrentRefreshToken(httpContext)),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid id,
        IEnumerable<IValidator<RevokeSessionCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new RevokeSessionCommand(id);
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> RevokeOtherSessionsAsync(
        HttpContext httpContext,
        IEnumerable<IValidator<RevokeOtherSessionsCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var currentRefreshToken = GetCurrentRefreshToken(httpContext);
        if (string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No se encontró la sesión actual.",
                extensions: new Dictionary<string, object?> { ["code"] = "current_session_not_found" });
        }

        var command = new RevokeOtherSessionsCommand(currentRefreshToken);
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : CreateProblemResult(result.Error);
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.ChangePassword.ChangePasswordCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.ChangePassword.ChangePasswordCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.ForgotPassword.ForgotPasswordCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.ForgotPassword.ForgotPasswordCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.Accepted();
        }
        return CreateProblemResult(result.Error);
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] GelyxLabs.Application.Features.Auth.ResetPassword.ResetPasswordCommand command,
        IEnumerable<IValidator<GelyxLabs.Application.Features.Auth.ResetPassword.ResetPasswordCommand>> validators,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(command, validators, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }
        return CreateProblemResult(result.Error);
    }

    private static IResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "invalid_credentials" => StatusCodes.Status401Unauthorized,
            "account_locked" => StatusCodes.Status423Locked,
            "account_inactive" => StatusCodes.Status403Forbidden,
            "token_invalid" => StatusCodes.Status401Unauthorized,
            "token_reused" => StatusCodes.Status401Unauthorized,
            "registration_disabled" => StatusCodes.Status404NotFound,
            "invalid_token" => StatusCodes.Status401Unauthorized,
            "password_change_failed" => StatusCodes.Status400BadRequest,
            "reset_failed" => StatusCodes.Status400BadRequest,
            "unauthorized" => StatusCodes.Status401Unauthorized,
            "session_not_found" => StatusCodes.Status404NotFound,
            "current_session_not_found" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static string? GetCurrentRefreshToken(HttpContext httpContext)
    {
        var cookieToken = httpContext.Request.Cookies["refreshToken"];
        if (!string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        var headerToken = httpContext.Request.Headers["X-Refresh-Token"].ToString();
        return string.IsNullOrWhiteSpace(headerToken) ? null : headerToken;
    }

    private static async Task<IResult?> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken cancellationToken)
    {
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(command, cancellationToken)));
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return errors.Count == 0
            ? null
            : Results.ValidationProblem(
                errors,
                statusCode: StatusCodes.Status400BadRequest,
                title: "La solicitud contiene errores de validación.");
    }
}

public sealed record UpdateUserPreferencesRequest(string Locale, string? Theme = null);

public sealed record TwoFactorCodeRequest(string Code);
public sealed record DisableTwoFactorRequest(string CurrentPassword, string Code);
public sealed record LoginTwoFactorRequest(string TwoFactorToken, string Code);
