namespace Market.Application.Modules.Catalog.Products.Commands.Image.Upload;

public sealed class UploadProductImageCommandHandler(IAppDbContext ctx, IFileStorage fileStorage)
    : IRequestHandler<UploadProductImageCommand, UploadProductImageCommandDto>
{
    /// <summary>
    /// Sub-folder of the uploads root for product images (served as /uploads/products/...).
    /// </summary>
    private const string ProductImagesFolder = "products";

    public async Task<UploadProductImageCommandDto> Handle(UploadProductImageCommand request, CancellationToken ct)
    {
        // Size, type and file signature are checked by UploadProductImageCommandValidator.
        var product = await ctx.Products
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (product is null)
            throw new MarketNotFoundException($"Proizvod (ID={request.Id}) nije pronađen.");

        // The extension comes from the detected signature, never from the client's file name.
        var contentType = ImageFileSignature.DetectContentType(request.Content)!;
        var extension = ImageFileSignature.GetFileExtension(contentType);

        string newImageUrl;
        await using (var content = new MemoryStream(request.Content, writable: false))
        {
            newImageUrl = await fileStorage.SaveAsync(content, ProductImagesFolder, extension, ct);
        }

        var oldImageUrl = product.ImageUrl;
        product.ImageUrl = newImageUrl;

        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch
        {
            // The database still points to the old image, so the new file would be orphaned.
            await fileStorage.DeleteAsync(newImageUrl, CancellationToken.None);
            throw;
        }

        // The old file is deleted only after the database points to the new one.
        if (oldImageUrl is not null)
            await fileStorage.DeleteAsync(oldImageUrl, ct);

        return new UploadProductImageCommandDto { ImageUrl = newImageUrl };
    }
}
