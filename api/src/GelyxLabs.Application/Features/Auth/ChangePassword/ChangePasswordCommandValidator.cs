namespace GelyxLabs.Application.Features.Auth.ChangePassword;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Entities;

using FluentValidation;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
            .MinimumLength(10).WithMessage("La contraseña debe tener al menos 10 caracteres")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una minúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es obligatorio");
    }
}
