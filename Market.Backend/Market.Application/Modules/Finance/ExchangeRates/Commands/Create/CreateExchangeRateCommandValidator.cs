using Market.Domain.Entities.Finance;

namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Create;

public sealed class CreateExchangeRateCommandValidator : AbstractValidator<CreateExchangeRateCommand>
{
    public CreateExchangeRateCommandValidator(IAppDbContext ctx)
    {
        // Cascade.Stop: the database check runs only for a code of valid format.
        RuleFor(x => x.CurrencyCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Valuta je obavezna.")
            .Length(CurrencyEntity.Constraints.CodeLength).WithMessage("Oznaka valute mora imati tačno 3 slova (npr. USD).")
            .MustAsync(async (code, ct) =>
            {
                var normalized = code.Trim().ToUpperInvariant();
                return await ctx.Currencies.AnyAsync(c => c.Code == normalized && !c.IsBase, ct);
            })
            .WithMessage("Valuta ne postoji ili je bazna valuta (bazna valuta uvijek ima kurs 1).");

        RuleFor(x => x.RateToBase)
            .GreaterThan(0).WithMessage("Kurs mora biti veći od 0.")
            .PrecisionScale(ExchangeRateEntity.Constraints.RatePrecision, ExchangeRateEntity.Constraints.RateScale, ignoreTrailingZeros: true)
                .WithMessage($"Kurs može imati najviše {ExchangeRateEntity.Constraints.RateScale} decimala.");

        RuleFor(x => x.ValidFromUtc)
            .NotEmpty().WithMessage("Datum od kada kurs važi je obavezan.");
    }
}
