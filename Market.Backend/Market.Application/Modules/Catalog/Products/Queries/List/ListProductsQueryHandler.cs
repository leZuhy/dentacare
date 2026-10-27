namespace Market.Application.Modules.Catalog.Products.Queries.List;

public sealed class ListProductsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
        : IRequestHandler<ListProductsQuery, PageResult<ListProductsQueryDto>>
{
    public async Task<PageResult<ListProductsQueryDto>> Handle(
        ListProductsQuery request, CancellationToken ct)
    {
        var q = ctx.Products.AsNoTracking();

        // Visitors and customers see only what can be ordered: enabled products in enabled categories.
        // The administrator sees everything (the admin product list also shows disabled products).
        if (!currentUser.IsAdmin)
        {
            q = q.Where(x => x.IsEnabled && x.Category!.IsEnabled);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(searchTerm));
        }

        if (request.CategoryId is not null)
        {
            q = q.Where(x => x.CategoryId == request.CategoryId);
        }

        // ThenBy(Id) makes the order stable (Name is not unique), so paging never skips or repeats rows.
        var projectedQuery = q.OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new ListProductsQueryDto
            {
                Id = x.Id,
                Name = x.Name,
                IsEnabled = x.IsEnabled,
                Description = x.Description,
                Price = x.Price,
                StockQuantity = x.StockQuantity,
                CategoryName = x.Category!.Name,
                ImageUrl = x.ImageUrl
            });

        return await PageResult<ListProductsQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
