namespace Market.Application.Modules.Catalog.Products.Commands.Image.Upload;

public sealed class UploadProductImageCommandDto
{
    /// <summary>
    /// Relative URL of the stored image, e.g. <c>/uploads/products/{guid}.webp</c>.
    /// </summary>
    public required string ImageUrl { get; init; }
}
