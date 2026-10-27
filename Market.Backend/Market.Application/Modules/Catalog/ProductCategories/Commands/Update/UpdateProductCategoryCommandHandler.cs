namespace Market.Application.Modules.Catalog.ProductCategories.Commands.Update;

public sealed class UpdateProductCategoryCommandHandler(IAppDbContext ctx)
            : IRequestHandler<UpdateProductCategoryCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductCategoryCommand request, CancellationToken ct)
    {
        var entity = await ctx.ProductCategories
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            throw new MarketNotFoundException($"Kategorija (ID={request.Id}) nije pronađena.");

        var normalized = request.Name.Trim();

        // Check for duplicate name (case-insensitive, except for the same ID)
        var exists = await ctx.ProductCategories
            .AnyAsync(x => x.Id != request.Id && x.Name.ToLower() == normalized.ToLower(), ct);

        if (exists)
        {
            throw new MarketConflictException($"Kategorija sa nazivom {normalized} već postoji.");
        }

        // Same business rule as DisableProductCategoryCommandHandler: a category with active products cannot be disabled.
        if (entity.IsEnabled && !request.IsEnabled)
        {
            var hasActiveProducts = await ctx.Products
                .AnyAsync(p => p.CategoryId == entity.Id && p.IsEnabled, ct);

            if (hasActiveProducts)
                throw new MarketBusinessRuleException("category.disable.blocked.activeProducts",
                    $"Kategorija {entity.Name} se ne može onemogućiti jer sadrži aktivne proizvode.");
        }

        entity.Name = normalized;
        entity.IsEnabled = request.IsEnabled;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
