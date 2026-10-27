namespace Market.Application.Modules.Finance.ExchangeRates.Queries.List;

public sealed class ListExchangeRatesQueryDto
{
    public required int Id { get; init; }
    public required string CurrencyCode { get; init; }
    public required decimal RateToBase { get; init; }
    public required DateTime ValidFromUtc { get; init; }
    public required string Source { get; init; }
}
