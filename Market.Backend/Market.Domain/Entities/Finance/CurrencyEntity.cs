using Market.Domain.Common;

namespace Market.Domain.Entities.Finance;

/// <summary>
/// Represents a currency (ISO 4217) that prices can be converted to.
/// Exactly one currency is the base currency (BAM): all exchange rates are stored relative to it.
/// </summary>
public class CurrencyEntity : BaseEntity
{
    /// <summary>
    /// ISO 4217 code in upper case, e.g. "BAM", "EUR".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name, e.g. "Konvertibilna marka".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display symbol, e.g. "KM", "€".
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Number of decimal places that converted amounts are rounded to.
    /// </summary>
    public int DecimalPlaces { get; set; }

    /// <summary>
    /// True for the base currency (BAM). Its rate to itself is always 1, so it has no stored exchange rates.
    /// </summary>
    public bool IsBase { get; set; }

    /// <summary>
    /// Only enabled currencies are listed and can be used for conversion.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Exchange rate history of this currency (read-only navigation, rates are added through <see cref="ExchangeRateEntity"/>).
    /// </summary>
    public IReadOnlyCollection<ExchangeRateEntity> ExchangeRates { get; private set; } = new List<ExchangeRateEntity>();

    /// <summary>
    /// Single source of truth for technical/business constraints.
    /// Used in validators and EF configuration.
    /// </summary>
    public static class Constraints
    {
        public const int CodeLength = 3;

        public const int NameMaxLength = 50;

        public const int SymbolMaxLength = 10;
    }
}
