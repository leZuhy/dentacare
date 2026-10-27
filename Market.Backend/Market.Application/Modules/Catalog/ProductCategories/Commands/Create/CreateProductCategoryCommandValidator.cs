namespace Market.Application.Modules.Catalog.ProductCategories.Commands.Create;

public sealed class CreateProductCategoryCommandValidator
    : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(ProductCategoryEntity.Constraints.NameMaxLength).WithMessage($"Naziv može imati najviše {ProductCategoryEntity.Constraints.NameMaxLength} znakova.");
    }
}
