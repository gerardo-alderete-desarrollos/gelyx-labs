namespace GelyxLabs.Application.Features.Auth.Register;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName) : ICommand<RegisterResponse>;

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string FullName);
