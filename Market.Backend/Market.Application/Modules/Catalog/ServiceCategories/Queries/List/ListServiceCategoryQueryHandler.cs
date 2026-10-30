namespace Market.Application.Modules.Catalog.ServiceCategories.Queries.List;

public sealed class ListServiceCategoryQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListServiceCategoryQuery, PageResult<ListServiceCategoryQueryDto>>
{
    public async Task<PageResult<ListServiceCategoryQueryDto>> Handle(ListServiceCategoryQuery request, CancellationToken ct)
    {
        // Read only, so AsNoTracking is safe and faster
        var q = ctx.ServiceCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(search));
        }

        var projectedQuery = q.OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new ListServiceCategoryQueryDto
            {
                Id = x.Id,
                Name = x.Name
            });

        return await PageResult<ListServiceCategoryQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
