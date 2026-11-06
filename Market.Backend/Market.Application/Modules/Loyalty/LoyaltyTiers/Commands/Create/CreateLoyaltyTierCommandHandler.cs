using Market.Domain.Entities.Loyalty;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Create;

public sealed class CreateLoyaltyTierCommandHandler(IAppDbContext ctx) : IRequestHandler<CreateLoyaltyTierCommand, int>
{
    public async Task<int> Handle(CreateLoyaltyTierCommand request, CancellationToken ct)
    {
        var tier = new LoyaltyTierEntity
        {
            Name = request.Name.Trim(),
            MinPoints = request.MinPoints,
            DiscountPercent = request.DiscountPercent
        };

        ctx.LoyaltyTiers.Add(tier);
        await ctx.SaveChangesAsync(ct);

        return tier.Id;
    }
}
