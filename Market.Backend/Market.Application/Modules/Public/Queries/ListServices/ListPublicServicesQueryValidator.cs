using Market.Domain.Entities.Catalog;

namespace Market.Application.Modules.Public.Queries.ListServices;

public sealed class ListPublicServicesQueryValidator : AbstractValidator<ListPublicServicesQuery>
{
    public ListPublicServicesQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(DentalServiceEntity.Constraints.NameMaxLength)
            .WithMessage($"Pretraga može imati najviše {DentalServiceEntity.Constraints.NameMaxLength} znakova.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Kategorija nije ispravna.")
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.MaxPriceBam)
            .GreaterThan(0)
            .WithMessage("Maksimalna cijena mora biti veća od 0 KM.")
            .LessThanOrEqualTo(DentalServiceEntity.Constraints.PriceMax)
            .WithMessage($"Maksimalna cijena može biti najviše {DentalServiceEntity.Constraints.PriceMax} KM.")
            .When(x => x.MaxPriceBam.HasValue);

        RuleFor(x => x.Sort)
            .IsInEnum()
            .WithMessage("Sortiranje nije ispravno.");
    }
}