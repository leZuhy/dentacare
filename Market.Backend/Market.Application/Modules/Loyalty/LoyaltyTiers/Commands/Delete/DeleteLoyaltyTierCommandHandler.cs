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

        // Deleting is a soft delete (an UPDATE), so the Restrict foreign key on LoyaltyAccounts.TierId never fires
        // the check must be done here.
        var isUsed = await ctx.LoyaltyAccounts
            .AnyAsync(a => a.TierId == request.Id, ct);

        if (isUsed)
            throw new MarketConflictException("Nivo koriste loyalty računi pacijenata i ne može biti obrisan.");


        ctx.LoyaltyTiers.Remove(tier);
        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
