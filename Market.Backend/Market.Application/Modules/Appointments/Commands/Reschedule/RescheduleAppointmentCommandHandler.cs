using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;

namespace Market.Application.Modules.Appointments.Commands.Reschedule;

public sealed class RescheduleAppointmentCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser,
    TimeProvider timeProvider, IOptions<ClinicOptions> clinicOptions, AppointmentSlotChecker slotChecker)
    : IRequestHandler<RescheduleAppointmentCommand, Unit>
{
    public async Task<Unit> Handle(RescheduleAppointmentCommand request, CancellationToken ct)
    {
        // 1. Who is moving: always from the access token
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // 2. Only own appointment (IDOR), checked inside the query.
        //    Somebody else's appointment gives the same 404 as a missing one.
        //    Tracked (no AsNoTracking), because StartUtc and EndUtc are changed below.
        var appointment = await ctx.Appointments
            .Where(x => x.Id == request.Id && x.Patient.UserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new MarketNotFoundException("Termin nije pronađen.");

        // 3. Only a scheduled appointment can be moved
        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new MarketConflictException("Može se pomjeriti samo zakazan termin.");

        // 4. At least N hours before the OLD start (same rule as cancelling)
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var minHours = clinicOptions.Value.CancellationMinHoursBefore;

        if (!AppointmentCancellationPolicy.CanCancel(appointment.StartUtc, nowUtc, minHours))
            throw new MarketConflictException($"Termin se može pomjeriti najkasnije {minHours} h prije početka.");

        // 5. The new time must be different
        if (request.StartUtc == appointment.StartUtc)
            throw new MarketConflictException("Novo vrijeme je isto kao staro. Izaberite drugo vrijeme.");

        // 6. Dentist of this appointment, with an enabled account.
        //    Tracked, because ScheduleVersion is changed before saving.
        var dentist = await ctx.Dentists
            .FirstOrDefaultAsync(x => x.Id == appointment.DentistId && x.User.IsEnabled, ct)
            ?? throw new MarketConflictException("Stomatolog trenutno ne prima termine.");

        // 7. Same duration as before (not the current duration of the service)
        var durationMinutes = (int)(appointment.EndUtc - appointment.StartUtc).TotalMinutes;

        // 8. Same rules as booking. The appointment being moved doesn't block itself.
        var endUtc = await slotChecker.EnsureCanTakeAsync(appointment.PatientId, dentist.Id,
            request.StartUtc, durationMinutes, excludeAppointmentId: appointment.Id, ct);

        // 9. Move it and change the schedule version (optimistic concurrency, #13)
        appointment.StartUtc = request.StartUtc;
        appointment.EndUtc = endUtc;
        dentist.ScheduleVersion = Guid.NewGuid();

        // 10. One SaveChangesAsync = one transaction
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another booking or move for this dentist at the same moment,
            // or the unique index (DentistId, StartUtc).
            throw new MarketConflictException("Termin je upravo zauzet. Izaberite drugi.");
        }

        return Unit.Value;
    }
}