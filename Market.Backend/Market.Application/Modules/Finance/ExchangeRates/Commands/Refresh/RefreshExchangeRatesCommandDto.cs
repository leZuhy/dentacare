namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Refresh;

public sealed class RefreshExchangeRatesCommandDto
{
    /// <summary>Number of new rates stored (a rate the database already has for the same date is not stored again).</summary>
    public required int Updated { get; init; }

    /// <summary>Where the rates come from, e.g. "Frankfurter (ECB)".</summary>
    public required string Source { get; init; }

    /// <summary>Codes of enabled currencies the provider does not publish (e.g. RSD), so they were not updated.</summary>
    public required List<string> Skipped { get; init; }
}
