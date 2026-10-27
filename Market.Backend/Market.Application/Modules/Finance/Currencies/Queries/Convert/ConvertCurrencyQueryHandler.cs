namespace Market.Application.Modules.Finance.Currencies.Queries.Convert;

public sealed class ConvertCurrencyQueryHandler(ICurrencyConverter currencyConverter)
    : IRequestHandler<ConvertCurrencyQuery, ConvertCurrencyQueryDto>
{
    public async Task<ConvertCurrencyQueryDto> Handle(ConvertCurrencyQuery request, CancellationToken ct)
    {
        // Unknown/disabled currency → 404, no rate for the date → 409 (thrown by the converter).
        var result = await currencyConverter.ConvertAsync(request.Amount, request.From, request.To, request.AtUtc, ct);

        return new ConvertCurrencyQueryDto
        {
            Amount = result.Amount,
            From = result.From,
            To = result.To,
            ConvertedAmount = result.ConvertedAmount,
            Rate = result.Rate,
            RateDateUtc = result.RateDateUtc
        };
    }
}
