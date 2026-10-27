using Market.Application.Abstractions;
using Market.Application.Common.Exceptions;
using Market.Domain.Entities.Finance;
using Market.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Market.Infrastructure.Common;

/// <summary>
/// Downloads the reference rates of the European Central Bank from the free Frankfurter API
/// (<c>GET {FrankfurterBaseUrl}/latest?from=EUR</c>, no API key). Enabled with <c>ExchangeRates:Provider = "Frankfurter"</c>.
/// <para>
/// ECB rates say how many units of a currency 1 EUR buys (e.g. 1 EUR = 1.17 USD). The KM is pegged to the euro
/// (1 EUR = 1.95583 KM), so the rate to the base currency is <c>1.95583 / 1.17 = 1.671650</c> KM per USD.
/// Currencies the ECB does not publish (e.g. RSD) are left out and reported by the refresh command.
/// </para>
/// </summary>
public sealed class FrankfurterExchangeRateProvider(
    HttpClient httpClient,
    IOptions<ExchangeRatesOptions> options,
    ILogger<FrankfurterExchangeRateProvider> logger) : IExchangeRateProvider
{
    private const string SourceName = "Frankfurter (ECB)";

    public bool IsEnabled => options.Value.Provider == ExchangeRatesOptions.ProviderFrankfurter;

    public async Task<ExchangeRatesSnapshot> GetLatestRatesToBaseAsync(
        IReadOnlyCollection<string> currencyCodes,
        CancellationToken ct = default)
    {
        FrankfurterLatestResponse? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<FrankfurterLatestResponse>("latest?from=EUR", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested)) // timeout
        {
            logger.LogWarning(ex, "Exchange rates could not be downloaded from {BaseAddress}.", httpClient.BaseAddress);
            throw Unavailable(ex);
        }

        if (response?.Rates is null)
            throw Unavailable(null);

        // EUR itself is not in the list ("from=EUR"): 1 EUR = 1 EUR.
        var unitsPerEuro = new Dictionary<string, decimal>(response.Rates, StringComparer.OrdinalIgnoreCase)
        {
            ["EUR"] = 1m
        };

        var ratesToBase = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in currencyCodes)
        {
            if (unitsPerEuro.TryGetValue(code, out var units) && units > 0)
            {
                ratesToBase[code] = Math.Round(
                    ExchangeRateEntity.EurRateToBam / units,
                    ExchangeRateEntity.Constraints.RateScale,
                    MidpointRounding.AwayFromZero);
            }
        }

        var validFromUtc = response.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return new ExchangeRatesSnapshot(SourceName, validFromUtc, ratesToBase);
    }

    private static MarketBusinessRuleException Unavailable(Exception? innerException)
    {
        return new MarketBusinessRuleException("exchangeRates.provider.unavailable",
            "Kursna lista nije preuzeta jer servis Frankfurter trenutno nije dostupan. Pokušajte ponovo kasnije.",
            innerException);
    }

    /// <summary>
    /// Response of <c>/latest?from=EUR</c>, e.g. <c>{ "amount": 1.0, "base": "EUR", "date": "2026-10-16", "rates": { "USD": 1.17, ... } }</c>.
    /// </summary>
    private sealed record FrankfurterLatestResponse(DateOnly Date, Dictionary<string, decimal>? Rates);
}
