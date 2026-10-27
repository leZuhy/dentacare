using Market.Domain.Entities.Finance;

namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Refresh;

public sealed class RefreshExchangeRatesCommandHandler(IAppDbContext ctx, IExchangeRateProvider provider)
    : IRequestHandler<RefreshExchangeRatesCommand, RefreshExchangeRatesCommandDto>
{
    public async Task<RefreshExchangeRatesCommandDto> Handle(RefreshExchangeRatesCommand request, CancellationToken ct)
    {
        if (!provider.IsEnabled)
            throw new MarketConflictException(
                "Automatsko preuzimanje kursne liste nije uključeno. " +
                "U appsettings.json postavite \"ExchangeRates\": { \"Provider\": \"Frankfurter\" } " +
                "(ili varijablu okruženja ExchangeRates__Provider=Frankfurter) i ponovo pokrenite API. " +
                "Do tada kurseve unosite ručno.");

        // The base currency (BAM) always has rate 1, so only the other enabled currencies are refreshed.
        var currencies = await ctx.Currencies
            .Where(c => c.IsEnabled && !c.IsBase)
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

        var snapshot = await provider.GetLatestRatesToBaseAsync(currencies.Select(c => c.Code).ToList(), ct);

        // A second refresh on the same day must not create duplicates (one rate per currency and date).
        var currencyIdsWithRateForDate = await ctx.ExchangeRates
            .Where(r => r.ValidFromUtc == snapshot.ValidFromUtc)
            .Select(r => r.CurrencyId)
            .ToListAsync(ct);

        var updated = 0;
        var skipped = new List<string>();

        foreach (var currency in currencies)
        {
            if (!snapshot.RatesToBase.TryGetValue(currency.Code, out var rateToBase))
            {
                skipped.Add(currency.Code);
                continue;
            }

            if (currencyIdsWithRateForDate.Contains(currency.Id))
                continue;

            ctx.ExchangeRates.Add(new ExchangeRateEntity
            {
                CurrencyId = currency.Id,
                RateToBase = rateToBase,
                ValidFromUtc = snapshot.ValidFromUtc,
                Source = snapshot.Source
            });
            updated++;
        }

        await ctx.SaveChangesAsync(ct);

        return new RefreshExchangeRatesCommandDto
        {
            Updated = updated,
            Source = snapshot.Source,
            Skipped = skipped
        };
    }
}
