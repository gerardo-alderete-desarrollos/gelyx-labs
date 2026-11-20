namespace GelyxLabs.Application.Features.Profile;

using GelyxLabs.Application.Abstractions.Cqrs;

public sealed record UpdateOwnProfileCommand(string FullName) : ICommand;
