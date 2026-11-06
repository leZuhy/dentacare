namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.List;

public sealed class ListLoyaltyTierQuery : BasePagedQuery<ListLoyaltyTierQueryDto>
{
    public string? Search { get; init; }
}
