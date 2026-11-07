namespace Market.Application.Modules.Public.Queries.ListServices;

public sealed class ListPublicServicesQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPublicServicesQuery, PageResult<ListPublicServicesQueryDto>>
{
    public async Task<PageResult<ListPublicServicesQueryDto>> Handle(ListPublicServicesQuery request, CancellationToken ct)
    {
        // the public sees only active services; deleted ones are hidden by the global filter
        var q = ctx.DentalServices
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            q = q.Where(x =>
                x.Name.ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)));
        }

        if (request.CategoryId is not null)
            q = q.Where(x => x.CategoryId == request.CategoryId);

        if (request.MaxPriceBam is not null)
            q = q.Where(x => x.PriceBam <= request.MaxPriceBam);

        var sorted = request.Sort switch
        {
            PublicServiceSort.PriceAsc => q.OrderBy(x => x.PriceBam),
            PublicServiceSort.PriceDesc => q.OrderByDescending(x => x.PriceBam),
            _ => q.OrderBy(x => x.Name)
        };

        var projectedQuery = sorted
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new ListPublicServicesQueryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                PriceBam = x.PriceBam,
                DurationMinutes = x.DurationMinutes
            });

        return await PageResult<ListPublicServicesQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}