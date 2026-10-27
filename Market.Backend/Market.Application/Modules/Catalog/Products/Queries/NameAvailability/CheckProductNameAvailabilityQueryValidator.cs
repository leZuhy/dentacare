namespace Market.Application.Modules.Catalog.Products.Queries.NameAvailability;

public sealed class CheckProductNameAvailabilityQueryValidator : AbstractValidator<CheckProductNameAvailabilityQuery>
{
    public CheckProductNameAvailabilityQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(ProductEntity.Constraints.NameMaxLength).WithMessage($"Naziv može imati najviše {ProductEntity.Constraints.NameMaxLength} znakova.");
    }
}
