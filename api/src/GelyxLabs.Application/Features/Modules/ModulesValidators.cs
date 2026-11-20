namespace GelyxLabs.Application.Features.Modules;

using FluentValidation;

public sealed class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public static readonly IReadOnlySet<string> AllowedIcons = new HashSet<string>(StringComparer.Ordinal)
    {
        "activity", "box", "boxes", "file-text", "key", "layout-dashboard", "settings",
        "shield", "users", "user", "workflow"
    };

    private static readonly string[] SupportedLocales = ["es", "en"];

    public UpdateModuleCommandValidator()
    {
        RuleFor(command => command.Translations)
            .NotNull()
            .Must(translations => SupportedLocales.All(translations.ContainsKey))
            .WithMessage("Translations must include es and en.");
        RuleForEach(command => command.Translations)
            .ChildRules(translation =>
            {
                translation.RuleFor(item => item.Key)
                    .Must(locale => SupportedLocales.Contains(locale, StringComparer.Ordinal))
                    .WithMessage("Unsupported locale.");
                translation.RuleFor(item => item.Value.Name)
                    .NotEmpty()
                    .Length(1, 60);
                translation.RuleFor(item => item.Value.Description)
                    .MaximumLength(500);
            });
        RuleFor(command => command.Icon)
            .Must(icon => icon is null || AllowedIcons.Contains(icon))
            .WithMessage("Icon is not allowed.");
        RuleFor(command => command.SortOrder).InclusiveBetween(0, 999);
    }
}
