namespace GelyxLabs.Application.Features.Auth.Me;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;

public sealed record MeQuery : IQuery<MeResponse>;

public sealed record MeResponse(
    Guid Id,
    string Email,
    string FullName,
    string[] Roles,
    string[] Permissions,
    ModuleInfo[] Modules,
    bool MustChangePassword,
    string Locale,
    string Theme);

public sealed record UpdateUserPreferencesCommand(string Locale, string? Theme) : ICommand;

public sealed record ModuleInfo(
    string Key,
    string Name,
    string? Icon,
    string? Route,
    int SortOrder);
