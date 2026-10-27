namespace Market.Application.Modules.Catalog.Products.Commands.Update;

public sealed class UpdateProductCommand : IRequest<Unit>
{
    [JsonIgnore]
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public required int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public required bool IsEnabled { get; set; }
}
