using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;

namespace Market.Application.Modules.Public.Queries.ListFreeSlots;

public sealed class ListPublicFreeSlotsQueryHandler(
    IAppDbContext ctx,
    TimeProvider timeProvider,
    IOptions<ClinicOptions> clinicOptions)
    : IRequestHandler<ListPublicFreeSlotsQuery, List<ListPublicFreeSlotsQueryDto>>
{
    public async Task<List<ListPublicFreeSlotsQueryDto>> Handle(ListPublicFreeSlotsQuery request, CancellationToken ct)
    {
        // 1. Dentist must exist and have an enabled account (deleted ones are hidden by the global filter)
        var dentistExists = await ctx.Dentists
            .AnyAsync(x => x.Id == request.DentistId && x.User.IsEnabled, ct);
        if (!dentistExists)
            throw new MarketNotFoundException("Stomatolog nije pronađen.");

        // 2. Service must exist and be active; only its duration is needed
        var durationMinutes = await ctx.DentalServices
            .Where(x => x.Id == request.DentalServiceId && x.IsActive)
            .Select(x => (int?)x.DurationMinutes)
            .FirstOrDefaultAsync(ct);
        if (durationMinutes is null)
            throw new MarketNotFoundException("Usluga nije pronađena.");

        // 3. Working hours of this dentist (local clinic time)
        var workingHours = await ctx.WorkingHours
            .AsNoTracking()
            .Where(x => x.DentistId == request.DentistId)
            .ToListAsync(ct);

        // 4. Clinic time zone (validated on startup)
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicOptions.Value.TimeZoneId);

        // 4b. Not more than MaxBookingDaysAhead days ahead, same rule as booking (#13).
        //     Slots that can't be booked are not shown.
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        var lastDate = today.AddDays(clinicOptions.Value.MaxBookingDaysAhead);

        if (request.FromDate > lastDate)
            return new List<ListPublicFreeSlotsQueryDto>();

        var toDate = request.ToDate ?? request.FromDate;
        if (toDate > lastDate)
            toDate = lastDate;

        // 5. Busy appointments that touch the range. The UTC bounds are wide on purpose
        //    (one extra day on each side), so they don't depend on converting midnight through the zone.
        var rangeStartUtc = DateTime.SpecifyKind(request.FromDate.AddDays(-1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var rangeEndUtc = DateTime.SpecifyKind(toDate.AddDays(2).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var busy = await ctx.Appointments
            .AsNoTracking()
            .Where(x => x.DentistId == request.DentistId
                        && x.Status != AppointmentStatus.Cancelled
                        && x.StartUtc < rangeEndUtc
                        && x.EndUtc > rangeStartUtc)
            .Select(x => new TimeSlot(x.StartUtc, x.EndUtc))
            .ToListAsync(ct);


        // 6. Calculate free slots and map them to DTOs
        var slots = FreeSlotCalculator.Calculate(
            request.FromDate,
            toDate,
            workingHours,
            busy,
            durationMinutes.Value,
            nowUtc,
            timeZone);

        return slots
            .Select(x => new ListPublicFreeSlotsQueryDto
            {
                StartUtc = x.StartUtc,
                EndUtc = x.EndUtc
            })
            .ToList();
    }
}
