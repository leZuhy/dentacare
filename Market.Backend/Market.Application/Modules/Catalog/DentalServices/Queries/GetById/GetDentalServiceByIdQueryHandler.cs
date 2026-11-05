namespace Market.Application.Modules.Catalog.DentalServices.Queries.GetById;

public sealed class GetDentalServiceByIdQueryHandler
    (IAppDbContext ctx): IRequestHandler<GetDentalServiceByIdQuery, GetDentalServiceByIdQueryDto>
{
    public async Task<GetDentalServiceByIdQueryDto> Handle(GetDentalServiceByIdQuery request, CancellationToken ct)
    {
        var dto = await ctx.DentalServices
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new GetDentalServiceByIdQueryDto
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                Name = x.Name,
                Description = x.Description,
                PriceBam = x.PriceBam,
                DurationMinutes = x.DurationMinutes,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(ct);
        
        if(dto is null)
            throw new MarketNotFoundException($"Usluga sa Id-em {request.Id} nije pronađena");

        return dto;
    }
}
