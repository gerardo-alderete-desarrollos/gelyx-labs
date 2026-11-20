namespace GelyxLabs.Application.Features.Auth.Logout;
using GelyxLabs.Domain.Errors;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Common;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var result = await _refreshTokenService.RevokeAsync(command.RefreshToken, "logout", cancellationToken);
        if (!result.Success)
        {
            return Result.Failure(new Error(result.ErrorCode ?? "token_invalid", "Token inválido"));
        }
        return Result.Success();
    }
}
