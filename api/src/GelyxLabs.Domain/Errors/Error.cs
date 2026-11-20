namespace GelyxLabs.Domain.Errors;

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");
    public static readonly Error NotFound = new("Error.NotFound", "Not found");
    public static readonly Error Unauthorized = new("Error.Unauthorized", "Unauthorized");
    public static readonly Error Forbidden = new("Error.Forbidden", "Forbidden");
    public static readonly Error Validation = new("Error.Validation", "Validation error");
    public static readonly Error Conflict = new("Error.Conflict", "Conflict");
}
