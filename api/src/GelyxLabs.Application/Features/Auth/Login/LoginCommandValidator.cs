namespace GelyxLabs.Application.Features.Auth.Login;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Entities;

using FluentValidation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El email no tiene un formato válido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria");
    }
}
