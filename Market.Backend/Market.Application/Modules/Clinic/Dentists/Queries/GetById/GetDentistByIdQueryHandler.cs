using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Queries.GetById
{
    public sealed class GetDentistByIdQueryHandler(IAppDbContext ctx) : IRequestHandler<GetDentistByIdQuery, GetDentistByQueryDto>
    {
        public async Task<GetDentistByQueryDto> Handle(GetDentistByIdQuery request, CancellationToken ct)
        {
            var dto = await ctx.Dentists
                .AsNoTracking()
                .Where(x => x.Id == request.Id)
                .Select(x => new GetDentistByQueryDto
                {
                    Id = x.Id,
                    Firstname = x.User.Firstname,
                    Lastname = x.User.Lastname,
                    Email = x.User.Email,
                    SpecializationId = x.SpecializationId,
                    SpecializationName = x.Specialization.Name,
                    Bio = x.Bio
                })
                .FirstOrDefaultAsync(ct);

            if(dto is null)
                throw new MarketNotFoundException($"Stomatolog sa Id-em {request.Id} nije pronađen");

            return dto;
        }
    }
}
