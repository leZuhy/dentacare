namespace Market.Application.Modules.Catalog.DentalServices.Commands.Update;

public sealed class UpdateDentalServiceCommand : IRequest<Unit>
{
    [JsonIgnore]
    public int Id { get; set; }
    public int CategoryId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal PriceBam { get; init; }
    public int DurationMinutes { get; init; }
    public bool IsActive { get; init; }
}
