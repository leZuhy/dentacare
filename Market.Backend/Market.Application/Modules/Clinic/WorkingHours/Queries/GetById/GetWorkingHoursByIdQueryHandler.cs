using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.WorkingHours.Queries.GetById
{
    public sealed class GetWorkingHoursByIdQueryHandler(IAppDbContext ctx) : IRequestHandler<GetWorkingHoursByIdQuery, GetWorkingHoursByIdQueryDto>
    {
        public async Task<GetWorkingHoursByIdQueryDto> Handle(GetWorkingHoursByIdQuery request, CancellationToken ct)
        {
            var dto = await ctx.WorkingHours
                .AsNoTracking()
                .Where(x => x.Id == request.Id)
                .Select(x => new GetWorkingHoursByIdQueryDto
                {
                    Id = x.Id,
                    DentistId = x.DentistId,
                    DentistFullName = x.Dentist.User.Firstname + " " + x.Dentist.User.Lastname,
                    DayOfWeek = x.DayOfWeek,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                })
                .FirstOrDefaultAsync(ct);

            if(dto is null)
                throw new MarketNotFoundException($"Radno vrijeme sa Id-em {request.Id} nije pronađeno");


            return dto;
        }
    }
}
