namespace Market.Application.Modules.Catalog.Products.Commands.Create;

public class CreateProductCommandHandler(IAppDbContext ctx)
    : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand request, CancellationToken ct)
    {
        // Input format (required name, lengths, price, stock) and the unique name
        // are checked by CreateProductCommandValidator.
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

        var product = new ProductEntity
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            IsEnabled = request.IsEnabled
        };

        ctx.Products.Add(product);
        await ctx.SaveChangesAsync(ct);

        return product.Id;
    }
}
