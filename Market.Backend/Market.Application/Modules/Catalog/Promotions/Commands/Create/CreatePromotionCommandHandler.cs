namespace Market.Application.Modules.Catalog.Promotions.Commands.Create;

public sealed class CreatePromotionCommandHandler(IAppDbContext ctx)
    : IRequestHandler<CreatePromotionCommand, int>
{
    public async Task<int> Handle(CreatePromotionCommand request, CancellationToken ct)
    {
        // Input format (lengths, URLs, sort order, date range) is checked by CreatePromotionCommandValidator.
        var promotion = new PromotionEntity
        {
            Title = request.Title.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            TargetUrl = string.IsNullOrWhiteSpace(request.TargetUrl) ? null : request.TargetUrl.Trim(),
            IsActive = request.IsActive,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            SortOrder = request.SortOrder
        };

        ctx.Promotions.Add(promotion);
        await ctx.SaveChangesAsync(ct);

        return promotion.Id;
    }
}
