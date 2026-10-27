namespace Market.Application.Modules.Catalog.Products.Commands.Image.Delete;

public sealed class DeleteProductImageCommandHandler(IAppDbContext ctx, IFileStorage fileStorage)
    : IRequestHandler<DeleteProductImageCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductImageCommand request, CancellationToken ct)
    {
        var product = await ctx.Products
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (product is null)
            throw new MarketNotFoundException($"Proizvod (ID={request.Id}) nije pronađen.");

        // Idempotent — a product without an image stays as it is.
        if (product.ImageUrl is null)
            return Unit.Value;

        var oldImageUrl = product.ImageUrl;
        product.ImageUrl = null;
        await ctx.SaveChangesAsync(ct);

        // The file is deleted only after the database no longer points to it.
        await fileStorage.DeleteAsync(oldImageUrl, ct);

        return Unit.Value;
    }
}
