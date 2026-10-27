namespace Market.Application.Modules.Catalog.Promotions.Commands.Update;

public sealed class UpdatePromotionCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdatePromotionCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePromotionCommand request, CancellationToken ct)
    {
        // Input format (lengths, URLs, sort order, date range) is checked by UpdatePromotionCommandValidator.
        var entity = await ctx.Promotions
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity is null)
            throw new MarketNotFoundException($"Promocija (ID={request.Id}) nije pronađena.");

        entity.Title = request.Title.Trim();
        entity.ImageUrl = request.ImageUrl.Trim();
        entity.TargetUrl = string.IsNullOrWhiteSpace(request.TargetUrl) ? null : request.TargetUrl.Trim();
        entity.IsActive = request.IsActive;
        entity.StartsAtUtc = request.StartsAtUtc;
        entity.EndsAtUtc = request.EndsAtUtc;
        entity.SortOrder = request.SortOrder;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
