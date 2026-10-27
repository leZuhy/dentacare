using Market.Domain.Entities.Finance;

namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Create;

public sealed class CreateExchangeRateCommandHandler(IAppDbContext ctx)
    : IRequestHandler<CreateExchangeRateCommand, int>
{
    /// <summary>
    /// Value of <see cref="ExchangeRateEntity.Source"/> for rates entered by the administrator.
    /// </summary>
    private const string ManualSource = "Ručni unos";

    public async Task<int> Handle(CreateExchangeRateCommand request, CancellationToken ct)
    {
        // Code format, existing non-base currency and rate > 0 are checked by CreateExchangeRateCommandValidator.
        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        var validFromUtc = request.ValidFromUtc.AsUtc();

        var currency = await ctx.Currencies
            .FirstOrDefaultAsync(c => c.Code == code && !c.IsBase, ct);

        if (currency is null)
            throw new MarketNotFoundException($"Valuta {code} nije pronađena.");

        // One rate per currency and moment (also guarded by a unique index).
        var exists = await ctx.ExchangeRates
            .AnyAsync(r => r.CurrencyId == currency.Id && r.ValidFromUtc == validFromUtc, ct);

        if (exists)
            throw new MarketConflictException($"Za valutu {code} već postoji kurs koji važi od {validFromUtc:dd.MM.yyyy. HH:mm} (UTC).");

        var rate = new ExchangeRateEntity
        {
            CurrencyId = currency.Id,
            RateToBase = request.RateToBase,
            ValidFromUtc = validFromUtc,
            Source = ManualSource
        };

        ctx.ExchangeRates.Add(rate);
        await ctx.SaveChangesAsync(ct);

        return rate.Id;
    }
}
