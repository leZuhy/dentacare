namespace Market.Application.Modules.Finance.Currencies.Queries.Convert;

public sealed class ConvertCurrencyQueryDto
{
    public required decimal Amount { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }

    /// <summary>Amount in the target currency, rounded to its decimal places.</summary>
    public required decimal ConvertedAmount { get; init; }

    /// <summary>Effective rate from → to, 6 decimals.</summary>
    public required decimal Rate { get; init; }

    /// <summary>Since when the applied rates are valid (UTC).</summary>
    public required DateTime RateDateUtc { get; init; }
}
