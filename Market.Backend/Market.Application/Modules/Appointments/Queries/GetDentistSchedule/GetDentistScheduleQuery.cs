using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Queries.GetDentistSchedule;
/// <summary>
/// Schedule of the logged-in dentist for one local clinic day: working hours and appointments.
/// There is no DentistId: the dentist is always taken from the access token (IDOR).
/// Not paged: one day has a limited number of appointments (same idea as free slots).
/// Query string: ?date=2026-11-23
/// </summary>
public sealed class GetDentistScheduleQuery : IRequest<GetDentistScheduleQueryDto>
{
    public DateOnly Date { get; init; }
}
