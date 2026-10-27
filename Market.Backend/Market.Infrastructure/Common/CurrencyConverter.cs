using Market.Application.Abstractions;
using Market.Application.Common;
using Market.Application.Common.Exceptions;

namespace Market.Infrastructure.Common;

/// <summary>
/// <see cref="ICurrencyConverter"/> that reads currencies and exchange rates from the database.
/// </summary>
public sealed class CurrencyConverter(IAppDbContext db, TimeProvider timeProvider) : ICurrencyConverter
{
    /// <summary>
    /// Decimal places of the effective rate returned to the client.
    /// </summary>
    private const int RateDecimalPlaces = 6;

    public async Task<CurrencyConversionResult> ConvertAsync(
        decimal amount,
        string fromCode,
        string toCode,
        DateTime? atUtc = null,
        CancellationToken ct = default)
    {
        var at = atUtc?.AsUtc() ?? timeProvider.GetUtcNow().UtcDateTime;

        var from = await GetRateToBaseAsync(fromCode, at, ct);
        var to = await GetRateToBaseAsync(toCode, at, ct);

        // Through the base currency: amount → KM → target currency. The full-precision rates are used,
        // and only the result is rounded (to the decimal places of the target currency).
        var convertedAmount = Math.Round(
            amount * from.RateToBase / to.RateToBase,
            to.DecimalPlaces,
            MidpointRounding.AwayFromZero);

        var rate = Math.Round(from.RateToBase / to.RateToBase, RateDecimalPlaces, MidpointRounding.AwayFromZero);

        // Both rates are valid since the later of their dates; base currency rates are always valid.
        var rateDateUtc = Later(from.ValidFromUtc, to.ValidFromUtc) ?? at;

        return new CurrencyConversionResult(
            amount,
            from.Code,
            to.Code,
            convertedAmount,
            rate,
            rateDateUtc,
            from.RateToBase,
            to.RateToBase);
    }

    private async Task<CurrencyRate> GetRateToBaseAsync(string code, DateTime atUtc, CancellationToken ct)
    {
        // Codes are stored in upper case, so the lookup is case-insensitive in every database provider.
        var normalizedCode = code.Trim().ToUpperInvariant();

        var currency = await db.Currencies
            .AsNoTracking()
            .Where(c => c.Code == normalizedCode && c.IsEnabled)
            .Select(c => new { c.Id, c.Code, c.DecimalPlaces, c.IsBase })
            .FirstOrDefaultAsync(ct);

        if (currency is null)
            throw new MarketNotFoundException($"Valuta {normalizedCode} nije pronađena ili nije omogućena.");

        if (currency.IsBase)
            return new CurrencyRate(currency.Code, currency.DecimalPlaces, 1m, null);

        // The rate valid at "atUtc" is the latest one that started on or before that moment.
        var rate = await db.ExchangeRates
            .AsNoTracking()
            .Where(r => r.CurrencyId == currency.Id && r.ValidFromUtc <= atUtc)
            .OrderByDescending(r => r.ValidFromUtc)
            .ThenByDescending(r => r.Id)
            .Select(r => new { r.RateToBase, r.ValidFromUtc })
            .FirstOrDefaultAsync(ct);

        if (rate is null)
            throw new MarketBusinessRuleException("currency.rate.notFound",
                $"Za valutu {currency.Code} ne postoji kurs koji važi na dan {atUtc:dd.MM.yyyy.}");

        return new CurrencyRate(currency.Code, currency.DecimalPlaces, rate.RateToBase, rate.ValidFromUtc);
    }

    private static DateTime? Later(DateTime? first, DateTime? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return first > second ? first : second;
    }

    /// <summary>
    /// Rate of one currency to the base currency; <c>ValidFromUtc</c> is null for the base currency (its rate 1 has no date).
    /// </summary>
    private sealed record CurrencyRate(string Code, int DecimalPlaces, decimal RateToBase, DateTime? ValidFromUtc);
}
