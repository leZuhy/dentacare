namespace Market.Application.Modules.Catalog.DentalServices.Queries.List;

public sealed class ListDentalServiceQueryDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public string Name { get; init; } = null!;
    public decimal PriceBam { get; init; }
    public int DurationMinutes { get; init; }
    public bool IsActive { get; init; }
}
