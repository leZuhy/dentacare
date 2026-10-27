namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Create;

/// <summary>
/// Adds a new rate for a currency (rates are never edited; a new rate replaces the old one from its <see cref="ValidFromUtc"/>).
/// </summary>
public sealed class CreateExchangeRateCommand : IRequest<int>
{
    /// <summary>Currency code, e.g. "USD" (case-insensitive). The base currency cannot get a rate.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>How many KM one unit of the currency is worth (greater than 0, at most 6 decimals).</summary>
    public decimal RateToBase { get; set; }

    /// <summary>The rate is valid from this moment (UTC).</summary>
    public DateTime ValidFromUtc { get; set; }
}
