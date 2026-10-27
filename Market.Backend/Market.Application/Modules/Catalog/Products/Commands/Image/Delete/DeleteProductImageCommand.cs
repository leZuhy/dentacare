namespace Market.Application.Modules.Catalog.Products.Commands.Image.Delete;

public sealed class DeleteProductImageCommand : IRequest<Unit>
{
    public required int Id { get; set; }
}
