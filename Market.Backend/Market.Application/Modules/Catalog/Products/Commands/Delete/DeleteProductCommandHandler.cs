namespace Market.Application.Modules.Catalog.Products.Commands.Delete;

public class DeleteProductCommandHandler(IAppDbContext context, IAppCurrentUser appCurrentUser)
      : IRequestHandler<DeleteProductCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // Defense in depth: the endpoint is already protected by the "Admin" policy.
        if (!appCurrentUser.IsAdmin)
            throw new MarketForbiddenException("Samo administrator može brisati proizvode.");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product is null)
            throw new MarketNotFoundException("Proizvod nije pronađen.");

        // Soft delete never reaches the database as DELETE, so the FK (Restrict) cannot protect us here.
        // A product that appears in orders must stay, otherwise the order history would point to a deleted product.
        var isUsedInOrders = await context.OrderItems
            .AnyAsync(oi => oi.ProductId == product.Id, cancellationToken);

        if (isUsedInOrders)
            throw new MarketConflictException(
                $"Proizvod {product.Name} se ne može obrisati jer se nalazi u narudžbama. " +
                "Umjesto brisanja, onemogućite proizvod.");

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
