namespace GelyxLabs.Application.Abstractions.Email;

using System.Net;

public static class EmailTemplates
{
    public static EmailMessage PasswordReset(string to, string locale, string resetLink)
    {
        var isEnglish = IsEnglish(locale);
        var subject = isEnglish ? "Reset your password - GelyxLabs" : "Restablece tu contraseña - GelyxLabs";
        var heading = isEnglish ? "Reset your password" : "Restablece tu contraseña";
        var greeting = isEnglish ? "You requested a password reset." : "Solicitaste restablecer tu contraseña.";
        var action = isEnglish ? "Reset password" : "Restablecer contraseña";
        var expiration = isEnglish ? "This link expires in 24 hours." : "Este enlace expira en 24 horas.";
        var ignore = isEnglish
            ? "If you did not request this, you can safely ignore this email."
            : "Si no solicitaste esto, puedes ignorar este correo.";

        return new EmailMessage(
            to,
            subject,
            $"""<html><body><h1>{heading}</h1><p>{greeting}</p><p><a href="{WebUtility.HtmlEncode(resetLink)}">{action}</a></p><p>{expiration}</p><p>{ignore}</p></body></html>""",
            NormalizeLocale(locale));
    }

    public static EmailMessage TemporaryPasswordWelcome(string to, string locale, string temporaryPassword)
    {
        var isEnglish = IsEnglish(locale);
        var subject = isEnglish ? "Welcome to GelyxLabs" : "Bienvenido a GelyxLabs";
        var heading = isEnglish ? "Welcome to GelyxLabs" : "Bienvenido a GelyxLabs";
        var copy = isEnglish
            ? "Your account is ready. Use the temporary password below and change it when you sign in."
            : "Tu cuenta está lista. Usa la siguiente contraseña temporal y cámbiala al iniciar sesión.";
        var passwordLabel = isEnglish ? "Temporary password" : "Contraseña temporal";

        return new EmailMessage(
            to,
            subject,
            $"""<html><body><h1>{heading}</h1><p>{copy}</p><p><strong>{passwordLabel}:</strong> {WebUtility.HtmlEncode(temporaryPassword)}</p></body></html>""",
            NormalizeLocale(locale));
    }

    private static bool IsEnglish(string locale) =>
        string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeLocale(string locale) => IsEnglish(locale) ? "en" : "es";
}
