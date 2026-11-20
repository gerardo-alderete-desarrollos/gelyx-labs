namespace GelyxLabs.Application.Features.Auth.ChangePassword;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string RefreshToken) : ICommand;
