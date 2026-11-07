namespace Market.Application.Modules.Public.Queries.ListServiceCategories;

public sealed class ListPublicServiceCategoriesQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPublicServiceCategoriesQuery, List<ListPublicServiceCategoriesQueryDto>>
{
    public async Task<List<ListPublicServiceCategoriesQueryDto>> Handle(ListPublicServiceCategoriesQuery request, CancellationToken ct)
    {
        return await ctx.ServiceCategories
            .AsNoTracking()
            .Where(x => x.DentalServices.Any(x => x.IsActive))
            .OrderBy(x => x.Name)
            .Select(x => new ListPublicServiceCategoriesQueryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(ct);
    }
}
