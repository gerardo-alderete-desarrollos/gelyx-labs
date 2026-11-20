namespace GelyxLabs.Application.Features.Permissions;

using FluentValidation;

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Permissions).NotNull();
        RuleForEach(command => command.Permissions)
            .NotEmpty()
            .MaximumLength(200);
    }
}
