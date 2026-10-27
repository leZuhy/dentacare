namespace Market.Application.Modules.Catalog.Promotions.Queries.GetById;

public sealed class GetPromotionByIdQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetPromotionByIdQuery, GetPromotionByIdQueryDto>
{
    public async Task<GetPromotionByIdQueryDto> Handle(GetPromotionByIdQuery request, CancellationToken ct)
    {
        var dto = await ctx.Promotions
            .Where(x => x.Id == request.Id)
            .Select(x => new GetPromotionByIdQueryDto
            {
                Id = x.Id,
                Title = x.Title,
                ImageUrl = x.ImageUrl,
                TargetUrl = x.TargetUrl,
                IsActive = x.IsActive,
                StartsAtUtc = x.StartsAtUtc,
                EndsAtUtc = x.EndsAtUtc,
                SortOrder = x.SortOrder
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            throw new MarketNotFoundException($"Promocija (ID={request.Id}) nije pronađena.");

        return dto;
    }
}
