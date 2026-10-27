namespace Market.Application.Modules.Catalog.Promotions.Queries.List;

public sealed class ListPromotionsQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPromotionsQuery, PageResult<ListPromotionsQueryDto>>
{
    public async Task<PageResult<ListPromotionsQueryDto>> Handle(ListPromotionsQuery request, CancellationToken ct)
    {
        var q = ctx.Promotions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            q = q.Where(x => x.Title.ToLower().Contains(searchTerm));
        }

        if (request.IsActive is not null)
        {
            q = q.Where(x => x.IsActive == request.IsActive);
        }

        // Same order as on the home page; ThenBy(Id) makes it stable for paging.
        var projectedQuery = q.OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new ListPromotionsQueryDto
            {
                Id = x.Id,
                Title = x.Title,
                ImageUrl = x.ImageUrl,
                TargetUrl = x.TargetUrl,
                IsActive = x.IsActive,
                StartsAtUtc = x.StartsAtUtc,
                EndsAtUtc = x.EndsAtUtc,
                SortOrder = x.SortOrder
            });

        return await PageResult<ListPromotionsQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
