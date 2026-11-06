namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.GetById;

public sealed class GetLoyaltyTierByIdQuery : IRequest<GetLoyaltyTierByIdQueryDto>
{
    public int Id { get; init; }
}
