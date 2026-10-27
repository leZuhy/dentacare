namespace Market.Application.Modules.Catalog.Products.Commands.Update;

public sealed class UpdateProductCommandHandler(IAppDbContext ctx)
            : IRequestHandler<UpdateProductCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        // Input format (required name, lengths, price, stock) and the unique name
        // are checked by UpdateProductCommandValidator.
        var entity = await ctx.Products
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            throw new MarketNotFoundException($"Proizvod (ID={request.Id}) nije pronađen.");

        var productCategory = await ctx.ProductCategories
          .Where(x => x.Id == request.CategoryId)
          .FirstOrDefaultAsync(ct);

        if (productCategory is null)
        {
            throw new MarketNotFoundException($"Kategorija (ID={request.CategoryId}) nije pronađena.");
        }

        if (!productCategory.IsEnabled)
        {
            throw new MarketBusinessRuleException("product.category.disabled",
                $"Kategorija {productCategory.Name} je onemogućena i ne može se dodijeliti proizvodu.");
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.Price = request.Price;
        entity.StockQuantity = request.StockQuantity;
        entity.CategoryId = request.CategoryId;
        entity.IsEnabled = request.IsEnabled;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
