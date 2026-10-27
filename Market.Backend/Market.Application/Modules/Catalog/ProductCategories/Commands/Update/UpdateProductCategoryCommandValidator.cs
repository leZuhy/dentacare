namespace Market.Application.Modules.Catalog.ProductCategories.Commands.Update;

public sealed class UpdateProductCategoryCommandValidator
    : AbstractValidator<UpdateProductCategoryCommand>
{
    public UpdateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(ProductCategoryEntity.Constraints.NameMaxLength).WithMessage($"Naziv može imati najviše {ProductCategoryEntity.Constraints.NameMaxLength} znakova.");
    }
}
