namespace Market.Application.Modules.Catalog.ProductCategories.Commands.Delete;

public class DeleteProductCategoryCommandHandler(IAppDbContext context, IAppCurrentUser appCurrentUser)
      : IRequestHandler<DeleteProductCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductCategoryCommand request, CancellationToken cancellationToken)
    {
        // Defense in depth: the endpoint is already protected by the "Admin" policy.
        if (!appCurrentUser.IsAdmin)
            throw new MarketForbiddenException("Samo administrator može brisati kategorije.");

        var category = await context.ProductCategories
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (category is null)
            throw new MarketNotFoundException("Kategorija nije pronađena.");

        // Soft delete never reaches the database as DELETE, so the FK (Restrict) cannot protect us here.
        // The rule has to be checked in code: a category that still has (non-deleted) products cannot be deleted.
        var hasProducts = await context.Products
            .AnyAsync(p => p.CategoryId == category.Id, cancellationToken);

        if (hasProducts)
            throw new MarketConflictException(
                $"Kategorija {category.Name} se ne može obrisati jer sadrži proizvode. " +
                "Premjestite proizvode u drugu kategoriju ili onemogućite kategoriju.");

        context.ProductCategories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
