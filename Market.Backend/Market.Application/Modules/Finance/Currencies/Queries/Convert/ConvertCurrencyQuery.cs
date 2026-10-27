namespace Market.Application.Modules.Finance.Currencies.Queries.Convert;

/// <summary>
/// Converts an amount between two currencies (query string: <c>?amount=100&amp;from=EUR&amp;to=BAM&amp;atUtc=2026-05-01T00:00:00Z</c>).
/// </summary>
public sealed class ConvertCurrencyQuery : IRequest<ConvertCurrencyQueryDto>
{
    /// <summary>Largest amount that can be converted (keeps the calculation far from decimal overflow).</summary>
    public const decimal MaxAmount = 1_000_000_000_000m;

    public decimal Amount { get; init; }

    /// <summary>Source currency code (case-insensitive), e.g. "EUR".</summary>
    public string From { get; init; } = string.Empty;

    /// <summary>Target currency code (case-insensitive), e.g. "BAM".</summary>
    public string To { get; init; } = string.Empty;

    /// <summary>Use the rates valid at this moment (UTC). Default: now. (optional)</summary>
    public DateTime? AtUtc { get; init; }
}
