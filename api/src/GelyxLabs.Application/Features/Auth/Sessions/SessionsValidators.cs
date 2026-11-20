namespace GelyxLabs.Application.Features.Auth.Sessions;

using FluentValidation;

public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class RevokeOtherSessionsCommandValidator : AbstractValidator<RevokeOtherSessionsCommand>
{
    public RevokeOtherSessionsCommandValidator()
    {
        RuleFor(command => command.CurrentRefreshToken).NotEmpty().MaximumLength(512);
    }
}
