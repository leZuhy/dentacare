namespace Market.Application.Abstractions;

/// <summary>
/// Converts amounts between currencies using the exchange rates stored in the database.
/// <para>
/// Formula: <c>converted = amount × fromRateToBase / toRateToBase</c>, rounded to the
/// decimal places of the target currency (<see cref="MidpointRounding.AwayFromZero"/>).
/// The rate used for a moment is the latest rate with <c>ValidFromUtc ≤ atUtc</c>; the base currency (BAM) always has rate 1.
/// </para>
/// </summary>
public interface ICurrencyConverter
{
    /// <summary>
    /// Converts <paramref name="amount"/> from <paramref name="fromCode"/> to <paramref name="toCode"/>
    /// (codes are case-insensitive) with the rates valid at <paramref name="atUtc"/> (default: now).
    /// </summary>
    /// <exception cref="Common.Exceptions.MarketNotFoundException">A currency does not exist or is disabled.</exception>
    /// <exception cref="Common.Exceptions.MarketBusinessRuleException">A currency has no rate valid at <paramref name="atUtc"/>.</exception>
    Task<CurrencyConversionResult> ConvertAsync(
        decimal amount,
        string fromCode,
        string toCode,
        DateTime? atUtc = null,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a currency conversion.
/// </summary>
/// <param name="Amount">The amount that was converted.</param>
/// <param name="From">Source currency code (upper case).</param>
/// <param name="To">Target currency code (upper case).</param>
/// <param name="ConvertedAmount">Amount in the target currency, rounded to its decimal places.</param>
/// <param name="Rate">Effective rate from → to (<c>FromRateToBase / ToRateToBase</c>), rounded to 6 decimals.</param>
/// <param name="RateDateUtc">
/// Moment from which the applied rates are valid: the later <c>ValidFromUtc</c> of the two rates.
/// When both currencies are the base currency (no stored rate is involved), it is the conversion moment.
/// </param>
/// <param name="FromRateToBase">Rate of the source currency to the base currency (1 for BAM).</param>
/// <param name="ToRateToBase">Rate of the target currency to the base currency (1 for BAM).</param>
public sealed record CurrencyConversionResult(
    decimal Amount,
    string From,
    string To,
    decimal ConvertedAmount,
    decimal Rate,
    DateTime RateDateUtc,
    decimal FromRateToBase,
    decimal ToRateToBase);
