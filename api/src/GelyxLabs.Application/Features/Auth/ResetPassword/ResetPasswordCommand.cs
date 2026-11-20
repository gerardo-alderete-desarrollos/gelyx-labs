namespace GelyxLabs.Application.Features.Auth.ResetPassword;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : ICommand;
