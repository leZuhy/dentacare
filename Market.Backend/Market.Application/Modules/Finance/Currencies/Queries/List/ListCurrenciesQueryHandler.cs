namespace Market.Application.Modules.Finance.Currencies.Queries.List;

public sealed class ListCurrenciesQueryHandler(IAppDbContext ctx, TimeProvider timeProvider)
    : IRequestHandler<ListCurrenciesQuery, List<ListCurrenciesQueryDto>>
{
    public async Task<List<ListCurrenciesQueryDto>> Handle(ListCurrenciesQuery request, CancellationToken ct)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var currencies = await ctx.Currencies
            .AsNoTracking()
            .Where(c => c.IsEnabled)
            .OrderByDescending(c => c.IsBase)
            .ThenBy(c => c.Code)
            .Select(c => new
            {
                c.Code,
                c.Name,
                c.Symbol,
                c.DecimalPlaces,
                c.IsBase,
                // The rate valid now = the latest rate that is already valid (future rates are skipped).
                CurrentRate = c.ExchangeRates
                    .Where(r => r.ValidFromUtc <= nowUtc)
                    .OrderByDescending(r => r.ValidFromUtc)
                    .ThenByDescending(r => r.Id)
                    .Select(r => new { r.RateToBase, r.ValidFromUtc })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        // The base currency has no stored rates: its rate to itself is always 1.
        return currencies
            .Select(c => new ListCurrenciesQueryDto
            {
                Code = c.Code,
                Name = c.Name,
                Symbol = c.Symbol,
                DecimalPlaces = c.DecimalPlaces,
                IsBase = c.IsBase,
                RateToBase = c.IsBase ? 1m : c.CurrentRate?.RateToBase,
                RateValidFromUtc = c.IsBase ? null : c.CurrentRate?.ValidFromUtc
            })
            .ToList();
    }
}
