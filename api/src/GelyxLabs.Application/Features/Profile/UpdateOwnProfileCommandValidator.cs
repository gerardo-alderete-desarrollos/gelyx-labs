namespace GelyxLabs.Application.Features.Profile;

using FluentValidation;

public sealed class UpdateOwnProfileCommandValidator : AbstractValidator<UpdateOwnProfileCommand>
{
    public UpdateOwnProfileCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .Must(fullName => !string.IsNullOrWhiteSpace(fullName))
            .WithMessage("El nombre es obligatorio.")
            .MaximumLength(200);
    }
}
