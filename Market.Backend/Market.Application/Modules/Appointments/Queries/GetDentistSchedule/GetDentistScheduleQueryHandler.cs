using Market.Application.Modules.Appointments.Common;
using Market.Shared.Options;
using Microsoft.Extensions.Options;

namespace Market.Application.Modules.Appointments.Queries.GetDentistSchedule;

/// <summary>
/// Schedule of the logged-in dentist for one local day: own working hours for that weekday
/// and own appointments that start on that day (all statuses, cancelled included).
/// </summary>
public sealed class GetDentistScheduleQueryHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IOptions<ClinicOptions> clinicOptions)
    : IRequestHandler<GetDentistScheduleQuery, GetDentistScheduleQueryDto>
{
    public async Task<GetDentistScheduleQueryDto> Handle(GetDentistScheduleQuery request, CancellationToken ct)
    {
        // 1. who is asking: always from the token, never from the request
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // 2. local clinic day -> UTC range [fromUtc, toUtc)
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicOptions.Value.TimeZoneId);
        var (fromUtc, toUtc) = ClinicDay.ToUtcRange(request.Date, timeZone);

        // 3. own working hours for that weekday (local time, nothing to convert)
        var workingHours = await ctx.WorkingHours
            .AsNoTracking()
            .Where(x => x.Dentist.UserId == userId && x.DayOfWeek == request.Date.DayOfWeek)
            .OrderBy(x => x.StartTime)
            .Select(x => new DentistScheduleWorkingHoursDto
            {
                StartTime = x.StartTime,
                EndTime = x.EndTime
            })
            .ToListAsync(ct);

        // 4. own appointments that START on that day; the owner check is in the query (IDOR)
        var appointments = await ctx.Appointments
            .AsNoTracking()
            .Where(x => x.Dentist.UserId == userId
                        && x.StartUtc >= fromUtc
                        && x.StartUtc < toUtc)
            .OrderBy(x => x.StartUtc)
            .ThenBy(x => x.Id)
            .Select(x => new DentistScheduleAppointmentDto
            {
                Id = x.Id,
                StartUtc = x.StartUtc,
                EndUtc = x.EndUtc,
                Status = x.Status,
                PatientFullName = x.Patient.User.Firstname + " " + x.Patient.User.Lastname,
                PatientPhone = x.Patient.Phone,
                DentalServiceNames = x.Services
                    .OrderBy(s => s.Id)
                    .Select(s => s.DentalService.Name)
                    .ToList(),
                TotalPriceBam = x.Services.Sum(s => s.PriceAtTimeBam)
            })
            .ToListAsync(ct);

        return new GetDentistScheduleQueryDto
        {
            Date = request.Date,
            WorkingHours = workingHours,
            Appointments = appointments
        };
    }
}