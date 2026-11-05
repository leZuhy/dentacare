namespace Market.Application.Modules.Catalog.DentalServices.Commands.Create;

public sealed class CreateDentalServiceCommand : IRequest<int>
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal PriceBam { get; init; }
    public int DurationMinutes { get; init; }
}
