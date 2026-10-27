namespace Market.Application.Modules.Catalog.Products.Commands.Update;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Id).GreaterThan(0);

        // Cascade.Stop: the database check runs only for a non-empty name of valid length.
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(ProductEntity.Constraints.NameMaxLength).WithMessage($"Naziv može imati najviše {ProductEntity.Constraints.NameMaxLength} znakova.")
            .MustAsync(async (command, name, ct) =>
            {
                // Case-insensitive, the product itself is skipped; soft-deleted products are skipped by the global query filter.
                var normalized = name.Trim().ToLower();
                return !await ctx.Products.AnyAsync(p => p.Id != command.Id && p.Name.ToLower() == normalized, ct);
            })
            .WithMessage("Proizvod sa ovim nazivom već postoji.");

        RuleFor(x => x.Description)
            .MaximumLength(ProductEntity.Constraints.DescriptionMaxLength).WithMessage($"Opis može imati najviše {ProductEntity.Constraints.DescriptionMaxLength} znakova.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Cijena ne može biti negativna.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Količina na stanju ne može biti negativna.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Kategorija je obavezna.");
    }
}
