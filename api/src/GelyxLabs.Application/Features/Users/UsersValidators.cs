namespace GelyxLabs.Application.Features.Users;

using FluentValidation;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status).Must(status =>
            status is null or "active" or "inactive" or "deleted");
        RuleFor(query => query.SortBy).Must(sortBy =>
            sortBy is null or "name" or "email" or "lastLoginAt");
        RuleFor(query => query.SortDir).Must(sortDir =>
            sortDir is null or "asc" or "desc");
    }
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.RoleIds).NotNull().NotEmpty();
    }
}

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.PhoneNumber).MaximumLength(32);
    }
}

public sealed class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.RoleIds).NotNull();
    }
}

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
