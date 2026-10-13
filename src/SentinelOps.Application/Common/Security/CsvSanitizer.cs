namespace SentinelOps.Application.Common.Security;

public static class CsvSanitizer
{
    private static readonly char[] DangerousPrefixes = ['=', '+', '-', '@', '\t', '\r'];

    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        var trimmed = value.TrimStart(' ');
        var result = value;
        if (trimmed.Length > 0 && DangerousPrefixes.Contains(trimmed[0]))
        {
            result = "'" + value;
        }

        var escaped = result.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
