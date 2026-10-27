namespace Market.Application.Modules.Finance.Currencies.Queries.List;

public sealed class ListCurrenciesQueryDto
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Symbol { get; init; }
    public required int DecimalPlaces { get; init; }
    public required bool IsBase { get; init; }

    /// <summary>KM per one unit, valid now: 1 for the base currency, null when the currency has no rate yet.</summary>
    public required decimal? RateToBase { get; init; }

    /// <summary>Since when the current rate is valid; null for the base currency and for a currency without a rate.</summary>
    public required DateTime? RateValidFromUtc { get; init; }
}
