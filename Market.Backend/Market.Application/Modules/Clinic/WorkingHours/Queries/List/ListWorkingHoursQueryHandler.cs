using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.WorkingHours.Queries.List
{
    public sealed class ListWorkingHoursQueryHandler(IAppDbContext ctx)
        :IRequestHandler<ListWorkingHoursQuery, PageResult<ListWorkingHoursQueryDto>>
    {
        public async Task<PageResult<ListWorkingHoursQueryDto>> Handle(ListWorkingHoursQuery request, CancellationToken ct)
        {
            var q = ctx.WorkingHours.AsNoTracking();

            if (request.DentistId is not null)
                q = q.Where(x => x.DentistId == request.DentistId);

            if (request.DayOfWeek is not null)
                q = q.Where(x => x.DayOfWeek == request.DayOfWeek);

            var projectedQuery = q
                .OrderBy(x => x.Dentist.User.Lastname)
                .ThenBy(x => x.Dentist.User.Firstname)
                .ThenBy(x => x.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .ThenBy(x => x.Id)
                .Select(x => new ListWorkingHoursQueryDto
                {
                    Id = x.Id,
                    DentistId = x.DentistId,
                    DentistFullName = x.Dentist.User.Firstname + " " + x.Dentist.User.Lastname,
                    DayOfWeek = x.DayOfWeek,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                });

            return await PageResult<ListWorkingHoursQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
        }
    }
}
