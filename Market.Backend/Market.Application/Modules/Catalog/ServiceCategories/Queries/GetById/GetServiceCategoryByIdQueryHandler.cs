namespace Market.Application.Modules.Catalog.ServiceCategories.Queries.GetById;

public sealed class GetServiceCategoryByIdQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetServiceCategoryByIdQuery, GetServiceCategoryByIdQueryDto>
{
    public async Task<GetServiceCategoryByIdQueryDto> Handle(GetServiceCategoryByIdQuery request, CancellationToken ct)
    {
        var dto = await ctx.ServiceCategories
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new GetServiceCategoryByIdQueryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            throw new MarketNotFoundException($"Kategorija sa Id-em {request.Id} ne postoji.");

        return dto;
    }
}
