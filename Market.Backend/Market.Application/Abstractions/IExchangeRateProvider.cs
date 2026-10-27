namespace Market.Application.Abstractions;

/// <summary>
/// External source of current exchange rates (e.g. the ECB rates through the Frankfurter API).
/// Used only by the "refresh rates" admin command; conversions always read rates from the database.
/// <para>
/// Disabled by default (<c>ExchangeRates:Provider = "None"</c> in appsettings.json), so the application
/// never depends on the network unless the provider is explicitly enabled.
/// </para>
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// False when the provider is turned off in configuration.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Downloads the latest rates and returns, for each requested currency code the provider knows,
    /// its rate to the base currency (KM). Codes the provider does not publish are left out of the result.
    /// </summary>
    Task<ExchangeRatesSnapshot> GetLatestRatesToBaseAsync(
        IReadOnlyCollection<string> currencyCodes,
        CancellationToken ct = default);
}

/// <summary>
/// Rates downloaded from an <see cref="IExchangeRateProvider"/>.
/// </summary>
/// <param name="Source">Name stored in <c>ExchangeRateEntity.Source</c>, e.g. "Frankfurter (ECB)".</param>
/// <param name="ValidFromUtc">Date the rates were published for (UTC).</param>
/// <param name="RatesToBase">Currency code → how many KM one unit is worth.</param>
public sealed record ExchangeRatesSnapshot(
    string Source,
    DateTime ValidFromUtc,
    IReadOnlyDictionary<string, decimal> RatesToBase);
