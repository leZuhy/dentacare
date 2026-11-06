using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.GetById;

public sealed class GetLoyaltyTierByIdQueryHandler(IAppDbContext ctx) : IRequestHandler<GetLoyaltyTierByIdQuery, GetLoyaltyTierByIdQueryDto>
{
    public async Task<GetLoyaltyTierByIdQueryDto> Handle(GetLoyaltyTierByIdQuery request, CancellationToken ct)
    {
        var dto = await ctx.LoyaltyTiers
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new GetLoyaltyTierByIdQueryDto
            {
                Id = x.Id,
                Name = x.Name,
                MinPoints = x.MinPoints,
                DiscountPercent = x.DiscountPercent
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            throw new MarketNotFoundException($"Nivo sa Id-em {request.Id} nije pronaden");

        return dto;
    }
}
