namespace Market.Application.Common;

public static class StringExtensions
{
    /// <summary>
    /// Trimmed value, or null for null, "" and whitespace. For optional text fields: an empty form field
    /// is stored as null (no value), not as "".
    /// </summary>
    public static string? TrimToNull(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
