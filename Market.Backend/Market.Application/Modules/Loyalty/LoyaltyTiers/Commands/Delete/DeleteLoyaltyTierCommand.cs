namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Delete;

public sealed class DeleteLoyaltyTierCommand :  IRequest<Unit>
{
    public int Id { get; init; }
}
