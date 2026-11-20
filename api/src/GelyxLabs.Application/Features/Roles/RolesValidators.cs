namespace GelyxLabs.Application.Features.Roles;

using FluentValidation;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.ReassignToRoleId)
            .Must((command, target) => target != command.Id)
            .WithMessage("The target role must be different from the role being deleted.");
    }
}
