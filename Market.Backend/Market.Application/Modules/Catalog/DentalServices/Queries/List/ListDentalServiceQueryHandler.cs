namespace Market.Application.Modules.Catalog.DentalServices.Queries.List;

public sealed class ListDentalServiceQueryHandler(IAppDbContext ctx) : IRequestHandler<ListDentalServiceQuery, PageResult<ListDentalServiceQueryDto>>
{
    public async Task<PageResult<ListDentalServiceQueryDto>> Handle(ListDentalServiceQuery request, CancellationToken ct)
    {
        var q = ctx.DentalServices.AsNoTracking();

        if(!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            q = q.Where(x => x.Name.Contains(search));
        }

        if(request.CategoryId is not null)
            q = q.Where(x => x.CategoryId == request.CategoryId);

        if(request.IsActive is not null)
            q = q.Where(x => x.IsActive == request.IsActive);

        var projectedQuery = q
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new ListDentalServiceQueryDto
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                Name = x.Name,
                PriceBam = x.PriceBam,
                DurationMinutes = x.DurationMinutes,
                IsActive = x.IsActive
            });

        return await PageResult<ListDentalServiceQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);

    }
}
