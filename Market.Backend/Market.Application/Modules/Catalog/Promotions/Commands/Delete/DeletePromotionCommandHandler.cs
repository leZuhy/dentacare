namespace Market.Application.Modules.Catalog.Promotions.Commands.Delete;

public sealed class DeletePromotionCommandHandler(IAppDbContext ctx)
    : IRequestHandler<DeletePromotionCommand, Unit>
{
    public async Task<Unit> Handle(DeletePromotionCommand request, CancellationToken ct)
    {
        var entity = await ctx.Promotions
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity is null)
            throw new MarketNotFoundException($"Promocija (ID={request.Id}) nije pronađena.");

        // Soft delete (see DatabaseContext.ApplyAuditAndSoftDelete).
        ctx.Promotions.Remove(entity);
        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
