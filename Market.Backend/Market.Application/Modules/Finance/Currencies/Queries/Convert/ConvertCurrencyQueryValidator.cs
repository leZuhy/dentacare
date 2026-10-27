using Market.Domain.Entities.Finance;

namespace Market.Application.Modules.Finance.Currencies.Queries.Convert;

public sealed class ConvertCurrencyQueryValidator : AbstractValidator<ConvertCurrencyQuery>
{
    public ConvertCurrencyQueryValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Iznos ne može biti negativan.")
            .LessThanOrEqualTo(ConvertCurrencyQuery.MaxAmount).WithMessage("Iznos je prevelik.");

        RuleFor(x => x.From)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Izvorna valuta je obavezna.")
            .Length(CurrencyEntity.Constraints.CodeLength).WithMessage("Oznaka valute mora imati tačno 3 slova (npr. EUR).");

        RuleFor(x => x.To)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ciljna valuta je obavezna.")
            .Length(CurrencyEntity.Constraints.CodeLength).WithMessage("Oznaka valute mora imati tačno 3 slova (npr. EUR).");
    }
}
