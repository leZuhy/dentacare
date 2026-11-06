namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Update;

public sealed class UpdateLoyaltyTierCommand : IRequest<Unit>
{
    [JsonIgnore]
    public int Id { get; set; }
    public string Name { get; init; } = null!;
    public int MinPoints { get; init; }
    public decimal DiscountPercent { get; init; }
}
