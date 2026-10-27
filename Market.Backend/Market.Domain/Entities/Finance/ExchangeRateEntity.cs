using Market.Domain.Common;

namespace Market.Domain.Entities.Finance;

/// <summary>
/// Exchange rate of one currency to the base currency (BAM), valid from <see cref="ValidFromUtc"/>
/// until the next rate of the same currency. Rates are never edited: a new rate is added instead,
/// so conversions for past dates keep using the rate that was valid at that time.
/// </summary>
public class ExchangeRateEntity : BaseEntity
{
    /// <summary>
    /// Fixed rate of the Central Bank of Bosnia and Herzegovina (currency board): 1 EUR = 1.95583 KM.
    /// </summary>
    public const decimal EurRateToBam = 1.95583m;

    /// <summary>
    /// Identifier of the currency this rate belongs to.
    /// </summary>
    public int CurrencyId { get; set; }

    /// <summary>
    /// Navigation reference to the currency.
    /// </summary>
    public CurrencyEntity? Currency { get; set; }

    /// <summary>
    /// How many units of the base currency (KM) one unit of this currency is worth, e.g. 1.95583 for EUR.
    /// </summary>
    public decimal RateToBase { get; set; }

    /// <summary>
    /// The rate is valid from this moment (UTC) until the next rate of the same currency.
    /// </summary>
    public DateTime ValidFromUtc { get; set; }

    /// <summary>
    /// Where the rate comes from, e.g. "Ručni unos", "Frankfurter (ECB)".
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Single source of truth for technical/business constraints.
    /// Used in validators and EF configuration.
    /// </summary>
    public static class Constraints
    {
        public const int RatePrecision = 18;

        public const int RateScale = 6;

        public const int SourceMaxLength = 50;
    }
}
