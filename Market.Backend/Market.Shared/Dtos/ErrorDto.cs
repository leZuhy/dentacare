namespace Market.Shared.Dtos;

/// <summary>
/// Standard API error response.
/// </summary>
public sealed class ErrorDto
{
    /// <summary>
    /// Application-level error code (e.g., "validation.error", "entity.error").
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// Human-readable message for the client.
    /// </summary>
    public string Message { get; set; } = default!;

    /// <summary>
    /// Unique trace identifier for debugging.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Detailed stack trace, included only in Development environment. (optional)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Field-level validation errors (only for <c>Code = "validation.error"</c>). (optional)
    /// Field names are camelCase paths, e.g. <c>"items[0].quantity"</c>, so the client can map them to form controls.
    /// </summary>
    public List<FieldErrorDto>? Errors { get; set; }
}
