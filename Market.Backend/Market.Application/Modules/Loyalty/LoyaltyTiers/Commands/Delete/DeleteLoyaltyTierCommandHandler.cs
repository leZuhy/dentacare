using Market.Domain.Entities.Loyalty;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Delete;

public sealed class DeleteLoyaltyTierCommandHandler(IAppDbContext ctx) : IRequestHandler<DeleteLoyaltyTierCommand, Unit>
{
    public async Task<Unit> Handle(DeleteLoyaltyTierCommand request, CancellationToken ct)
    {
        var tier = await ctx.LoyaltyTiers
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (tier is null)
            throw new MarketNotFoundException($"Nivo sa Id-em {request.Id} ne postoji");

        if (tier.MinPoints == LoyaltyTierEntity.Constraints.BaseTierMinPoints)
            throw new MarketConflictException("Pocetni nivo ne moze biti obrisan, jer ga dobija svaki novi pacijent");

        ctx.LoyaltyTiers.Remove(tier);
        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
