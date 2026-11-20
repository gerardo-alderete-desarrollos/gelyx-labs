namespace GelyxLabs.Application.Features.Auth.Login;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthResponse>;

public sealed record AuthResponse(
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    bool TwoFactorRequired = false,
    string? TwoFactorToken = null);
