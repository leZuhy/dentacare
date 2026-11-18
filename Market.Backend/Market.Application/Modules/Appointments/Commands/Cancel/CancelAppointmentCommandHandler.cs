using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;

namespace Market.Application.Modules.Appointments.Commands.Cancel;

public sealed class CancelAppointmentCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser,
    TimeProvider timeProvider, IOptions<ClinicOptions> clinicOptions)
    : IRequestHandler<CancelAppointmentCommand, Unit>
{
    public async Task<Unit> Handle(CancelAppointmentCommand request, CancellationToken ct)
    {
        // 1. Who is cancelling: always from the access token
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // 2. Only own appointment (IDOR). The owner check is part of the query.
        //    Somebody else's appointment gives the same 404 as a missing one.
        var query = ctx.Appointments.Where(x => x.Id == request.Id);

        query = currentUser.IsDentist
            ? query.Where(x => x.Dentist.UserId == userId)
            : query.Where(x => x.Patient.UserId == userId);

        // Tracked (no AsNoTracking), because the appointment is changed below
        var appointment = await query.FirstOrDefaultAsync(ct)
            ?? throw new MarketNotFoundException("Termin nije pronađen.");

        // 3. Only a scheduled appointment can be cancelled
        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new MarketConflictException("Može se otkazati samo zakazan termin.");

        // 4. At least N hours before the start (same rule for patient and dentist)
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var minHours = clinicOptions.Value.CancellationMinHoursBefore;

        if (!AppointmentCancellationPolicy.CanCancel(appointment.StartUtc, nowUtc, minHours))
            throw new MarketConflictException($"Termin se može otkazati najkasnije {minHours} h prije početka.");

        // 5. Cancel: status, time and who did it. The slot becomes free,
        //    because free slots ignore cancelled appointments (Status != Cancelled).
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAtUtc = nowUtc;
        appointment.CancelledByUserId = userId;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}