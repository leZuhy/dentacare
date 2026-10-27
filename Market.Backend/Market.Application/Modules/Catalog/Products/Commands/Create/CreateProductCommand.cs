namespace Market.Application.Modules.Catalog.Products.Commands.Create;

public class CreateProductCommand : IRequest<int>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }

    /// <summary>
    /// New products are enabled unless the client sends <c>false</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
