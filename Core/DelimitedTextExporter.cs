namespace WingetUSoft;

internal static class DelimitedTextExporter
{
    internal static string BuildRow(char separator, params string?[] values) =>
        string.Join(separator, values.Select(FormatField));

    internal static string FormatField(string? value)
    {
        string sanitized = (value ?? string.Empty)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        string trimmed = sanitized.TrimStart();
        if (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@')
            sanitized = "'" + sanitized;

        return $"\"{sanitized.Replace("\"", "\"\"")}\"";
    }
}