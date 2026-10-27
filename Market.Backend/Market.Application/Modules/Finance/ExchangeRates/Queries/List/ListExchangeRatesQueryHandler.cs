namespace Market.Application.Modules.Finance.ExchangeRates.Queries.List;

public sealed class ListExchangeRatesQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListExchangeRatesQuery, PageResult<ListExchangeRatesQueryDto>>
{
    public async Task<PageResult<ListExchangeRatesQueryDto>> Handle(ListExchangeRatesQuery request, CancellationToken ct)
    {
        var q = ctx.ExchangeRates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var code = request.CurrencyCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.Currency!.Code == code);
        }

        // Newest first; ThenByDescending(Id) makes the order stable for paging.
        var projectedQuery = q.OrderByDescending(x => x.ValidFromUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new ListExchangeRatesQueryDto
            {
                Id = x.Id,
                CurrencyCode = x.Currency!.Code,
                RateToBase = x.RateToBase,
                ValidFromUtc = x.ValidFromUtc,
                Source = x.Source
            });

        return await PageResult<ListExchangeRatesQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
