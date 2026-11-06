namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.List;

public sealed class ListLoyaltyTierQueryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public int MinPoints { get; init; }
    public decimal DiscountPercent { get; init; }
}
