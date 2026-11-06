namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Create;

public sealed class CreateLoyaltyTierCommand : IRequest<int>
{
    public string Name { get; init; } = null!;
    public int MinPoints { get; init; }
    public decimal DiscountPercent { get; init; }
}
