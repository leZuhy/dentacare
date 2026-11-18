using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;

namespace Market.Application.Modules.Appointments.Common;

/// <summary>
/// Shared rules for taking a time: booking (#13), rescheduling (#58)
/// and free slots for rescheduling.
/// excludeAppointmentId: the appointment being moved, so it doesn't block itself.
/// It is null when booking a new appointment.
/// </summary>
public sealed class AppointmentSlotChecker(IAppDbContext ctx, TimeProvider timeProvider,
    IOptions<ClinicOptions> clinicOptions)
{
    /// <summary>
    /// Free slots of one dentist for a duration, between two local clinic dates (both included).
    /// </summary>
    public async Task<List<TimeSlot>> GetFreeSlotsAsync(int dentistId, DateOnly fromDate, DateOnly toDate,
        int durationMinutes, int? excludeAppointmentId, CancellationToken ct)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicOptions.Value.TimeZoneId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var workingHours = await ctx.WorkingHours
            .AsNoTracking()
            .Where(x => x.DentistId == dentistId)
            .ToListAsync(ct);

        // Wide UTC bounds (one extra day on each side), same as in #12
        var rangeStartUtc = DateTime.SpecifyKind(fromDate.AddDays(-1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var rangeEndUtc = DateTime.SpecifyKind(toDate.AddDays(2).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var busy = await ctx.Appointments
            .AsNoTracking()
            .Where(x => x.DentistId == dentistId
                        && x.Status != AppointmentStatus.Cancelled
                        && (excludeAppointmentId == null || x.Id != excludeAppointmentId)
                        && x.StartUtc < rangeEndUtc
                        && x.EndUtc > rangeStartUtc)
            .Select(x => new TimeSlot(x.StartUtc, x.EndUtc))
            .ToListAsync(ct);

        return FreeSlotCalculator.Calculate(
            fromDate,
            toDate,
            workingHours,
            busy,
            durationMinutes,
            nowUtc,
            timeZone);
    }

    /// <summary>
    /// Checks that the patient can take this time with this dentist. Throws 409 if not.
    /// Returns the end of the appointment (start + duration).
    /// </summary>
    public async Task<DateTime> EnsureCanTakeAsync(int patientId, int dentistId, DateTime startUtc,
        int durationMinutes, int? excludeAppointmentId, CancellationToken ct)
    {
        // 1. Not more than MaxBookingDaysAhead days ahead (local clinic dates)
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicOptions.Value.TimeZoneId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var maxDays = clinicOptions.Value.MaxBookingDaysAhead;

        var startDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, timeZone));
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));

        if (startDate > today.AddDays(maxDays))
            throw new MarketConflictException($"Termin se moze zakazati najvise {maxDays} dana unaprijed");

        // 2. End of the appointment
        var endUtc = startUtc.AddMinutes(durationMinutes);

        // 3. The patient can't have two active appointments at the same time, with any dentist.
        //    Touching is allowed (9:00-10:00 and 10:00-10:30).
        var patientIsBusy = await ctx.Appointments
            .AnyAsync(x => x.PatientId == patientId
                           && x.Status != AppointmentStatus.Cancelled
                           && (excludeAppointmentId == null || x.Id != excludeAppointmentId)
                           && x.StartUtc < endUtc
                           && startUtc < x.EndUtc, ct);

        if (patientIsBusy)
            throw new MarketConflictException("Vec imate termin u to vrijeme");

        // 4. The start must still be a free slot (the list the patient saw may be old)
        var freeSlots = await GetFreeSlotsAsync(dentistId, startDate, startDate,
            durationMinutes, excludeAppointmentId, ct);

        if (!freeSlots.Any(x => x.StartUtc == startUtc))
            throw new MarketConflictException("Termin vise nije slobodan. Izaberite drugi");

        return endUtc;
    }
}