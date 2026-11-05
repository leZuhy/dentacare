namespace Market.Application.Modules.Catalog.DentalServices.Queries.GetById;

public sealed class GetDentalServiceByIdQueryDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal PriceBam { get; init; }
    public int DurationMinutes { get; init; }
    public bool IsActive { get; init; }
}
