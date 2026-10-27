using System.ComponentModel.DataAnnotations;

namespace Market.Shared.Options;

/// <summary>
/// "ExchangeRates" section: optional automatic download of exchange rates.
/// </summary>
public sealed class ExchangeRatesOptions
{
    public const string SectionName = "ExchangeRates";

    /// <summary>Rates are entered only manually (default).</summary>
    public const string ProviderNone = "None";

    /// <summary>Rates of the European Central Bank through the free Frankfurter API (no API key).</summary>
    public const string ProviderFrankfurter = "Frankfurter";

    /// <summary>
    /// "None" or "Frankfurter". With "None", <c>POST /Currencies/rates/refresh</c> returns 409
    /// and the application never calls an external service.
    /// </summary>
    [Required, AllowedValues(ProviderNone, ProviderFrankfurter)]
    public string Provider { get; init; } = ProviderNone;

    /// <summary>
    /// Base URL of the Frankfurter API.
    /// </summary>
    [Required, Url]
    public string FrankfurterBaseUrl { get; init; } = "https://api.frankfurter.app";
}
