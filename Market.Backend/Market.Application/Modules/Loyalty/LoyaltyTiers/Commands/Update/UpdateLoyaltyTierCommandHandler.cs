using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Update;

public sealed class UpdateLoyaltyTierCommandHandler(IAppDbContext ctx) : IRequestHandler<UpdateLoyaltyTierCommand, Unit>
{
    public async Task<Unit> Handle(UpdateLoyaltyTierCommand request, CancellationToken ct)
    {
        var tier = await ctx.LoyaltyTiers
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if(tier is null)
            throw new MarketNotFoundException($"Nivo lojalnosti s Id-em {request.Id} nije pronađen");

        tier.Name = request.Name.Trim();
        tier.MinPoints = request.MinPoints;
        tier.DiscountPercent = request.DiscountPercent;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
