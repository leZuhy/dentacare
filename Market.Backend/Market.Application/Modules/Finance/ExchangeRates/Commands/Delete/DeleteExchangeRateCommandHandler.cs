namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Delete;

public sealed class DeleteExchangeRateCommandHandler(IAppDbContext ctx)
    : IRequestHandler<DeleteExchangeRateCommand, Unit>
{
    public async Task<Unit> Handle(DeleteExchangeRateCommand request, CancellationToken ct)
    {
        var rate = await ctx.ExchangeRates
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (rate is null)
            throw new MarketNotFoundException($"Kurs (ID={request.Id}) nije pronađen.");

        // Soft delete: from now on the previous rate of the currency is used again for this period.
        ctx.ExchangeRates.Remove(rate);
        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
