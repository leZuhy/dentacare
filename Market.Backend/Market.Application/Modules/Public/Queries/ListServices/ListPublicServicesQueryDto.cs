namespace Market.Application.Modules.Public.Queries.ListServices;

public sealed class ListPublicServicesQueryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal PriceBam { get; init; }
    public required int DurationMinutes { get; init; }
}