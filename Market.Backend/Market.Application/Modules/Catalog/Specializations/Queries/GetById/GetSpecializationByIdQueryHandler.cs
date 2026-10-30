using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Queries.GetById
{
    public sealed class GetSpecializationByIdQueryHandler(IAppDbContext ctx) 
        : IRequestHandler<GetSpecializationByIdQuery, GetSpecializationByIdQueryDto>
    {
        public async Task<GetSpecializationByIdQueryDto> Handle(GetSpecializationByIdQuery request, CancellationToken ct)
        {
            var dto = await ctx.Specializations
                .AsNoTracking()
                .Where(x => x.Id == request.Id)
                .Select(x => new GetSpecializationByIdQueryDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .FirstOrDefaultAsync(ct);

            if(dto is null)
                throw new MarketNotFoundException($"Specijalizacija sa Id-em {request.Id} ne postoji.");

            return dto;
        }
    }
}
