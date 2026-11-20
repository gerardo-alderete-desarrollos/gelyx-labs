namespace GelyxLabs.Application.Features.Auth.Sessions;

using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Errors;

public sealed class GetSessionsHandler : IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionsRepository _repository;

    public GetSessionsHandler(ICurrentUserService currentUser, ISessionsRepository repository)
    {
        _currentUser = currentUser;
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(
        GetSessionsQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<IReadOnlyList<SessionDto>>(
                new Error("unauthorized", "No autenticado."));
        }

        return Result.Success(await _repository.GetActiveAsync(
            userId,
            query.CurrentRefreshToken,
            cancellationToken));
    }
}

public sealed class RevokeSessionHandler : ICommandHandler<RevokeSessionCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionsRepository _repository;
    private readonly IAuditLogger _auditLogger;

    public RevokeSessionHandler(
        ICurrentUserService currentUser,
        ISessionsRepository repository,
        IAuditLogger auditLogger)
    {
        _currentUser = currentUser;
        _repository = repository;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(new Error("unauthorized", "No autenticado."));
        }

        var revokedSession = await _repository.RevokeAsync(userId, command.Id, cancellationToken);
        if (revokedSession is null)
        {
            return Result.Failure(new Error("session_not_found", "Sesión no encontrada."));
        }

        await SessionAuditLogger.LogRevocationAsync(_auditLogger, revokedSession.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed class RevokeOtherSessionsHandler : ICommandHandler<RevokeOtherSessionsCommand, RevokeOtherSessionsResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionsRepository _repository;
    private readonly IAuditLogger _auditLogger;

    public RevokeOtherSessionsHandler(
        ICurrentUserService currentUser,
        ISessionsRepository repository,
        IAuditLogger auditLogger)
    {
        _currentUser = currentUser;
        _repository = repository;
        _auditLogger = auditLogger;
    }

    public async Task<Result<RevokeOtherSessionsResponse>> Handle(
        RevokeOtherSessionsCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<RevokeOtherSessionsResponse>(
                new Error("unauthorized", "No autenticado."));
        }

        var revokedSessions = await _repository.RevokeOthersAsync(
            userId,
            command.CurrentRefreshToken,
            cancellationToken);
        if (revokedSessions is null)
        {
            return Result.Failure<RevokeOtherSessionsResponse>(
                new Error("current_session_not_found", "La sesión actual no es válida."));
        }

        foreach (var session in revokedSessions)
        {
            await SessionAuditLogger.LogRevocationAsync(_auditLogger, session.Id, cancellationToken);
        }

        return Result.Success(new RevokeOtherSessionsResponse(revokedSessions.Count));
    }
}

file static class SessionAuditLogger
{
    public static Task LogRevocationAsync(
        IAuditLogger auditLogger,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        auditLogger.LogAsync(
            "session.revoked",
            "session",
            sessionId.ToString(),
            new Dictionary<string, object?> { ["sessionId"] = sessionId },
            cancellationToken);
}
