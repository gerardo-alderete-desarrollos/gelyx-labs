namespace GelyxLabs.Api.Common;

using System.Globalization;
using System.Text;

public static class CsvExport
{
    public static async Task WriteRowAsync(
        TextWriter writer,
        IEnumerable<string?> values,
        CancellationToken cancellationToken)
    {
        var escapedValues = values.Select(Escape);
        await writer.WriteLineAsync(string.Join(',', escapedValues).AsMemory(), cancellationToken);
    }

    public static string Date(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Escape(string? value)
    {
        var safeValue = value ?? string.Empty;
        var firstContentCharacter = safeValue.FirstOrDefault(character =>
            !char.IsWhiteSpace(character) && !char.IsControl(character));
        if (firstContentCharacter is '=' or '+' or '-' or '@')
        {
            safeValue = $"'{safeValue}";
        }

        return $"\"{safeValue.Replace("\"", "\"\"")}\"";
    }
}
