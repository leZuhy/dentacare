using System.Text.Json;

namespace Market.Shared.Dtos;

/// <summary>
/// Represents a single field-level validation error.
/// </summary>
public sealed class FieldErrorDto
{
    /// <summary>
    /// The name of the field that the error refers to.
    /// </summary>
    public string Field { get; set; } = default!;

    /// <summary>
    /// The validation error message.
    /// </summary>
    public string Message { get; set; } = default!;

    /// <summary>
    /// The error code defined in the validator. (optional)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Converts a server-side property path to the camelCase path used by the JSON client,
    /// one segment at a time: <c>"Items[0].Quantity"</c> → <c>"items[0].quantity"</c>.
    /// Uses the same naming policy as the JSON serializer, so field names match the JSON property names.
    /// A leading JSON-path root (<c>"$."</c>) from model binding errors is removed.
    /// </summary>
    public static string ToCamelCasePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "$")
            return string.Empty;

        if (path.StartsWith("$.", StringComparison.Ordinal))
            path = path[2..];

        var segments = path
            .Split('.')
            .Select(JsonNamingPolicy.CamelCase.ConvertName);

        return string.Join('.', segments);
    }
}
