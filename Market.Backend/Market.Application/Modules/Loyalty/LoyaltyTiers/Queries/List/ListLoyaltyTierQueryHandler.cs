namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.List;

public sealed class ListLoyaltyTierQueryHandler(IAppDbContext ctx) : IRequestHandler<ListLoyaltyTierQuery, PageResult<ListLoyaltyTierQueryDto>>
{
    public async Task<PageResult<ListLoyaltyTierQueryDto>> Handle(ListLoyaltyTierQuery request, CancellationToken ct)
    {
        var q = ctx.LoyaltyTiers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            q = q.Where(x => x.Name.Contains(search));
        }

        var projectedQuery = q
            .OrderBy(x => x.MinPoints)
            .Select(x => new ListLoyaltyTierQueryDto
            {
                Id = x.Id,
                Name = x.Name,
                MinPoints = x.MinPoints,
                DiscountPercent = x.DiscountPercent
            });

        return await PageResult<ListLoyaltyTierQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
