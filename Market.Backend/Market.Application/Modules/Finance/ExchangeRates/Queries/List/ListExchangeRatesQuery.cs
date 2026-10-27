namespace Market.Application.Modules.Finance.ExchangeRates.Queries.List;

public sealed class ListExchangeRatesQuery : BasePagedQuery<ListExchangeRatesQueryDto>
{
    /// <summary>Only rates of this currency, e.g. "USD" (case-insensitive). (optional)</summary>
    public string? CurrencyCode { get; init; }
}
