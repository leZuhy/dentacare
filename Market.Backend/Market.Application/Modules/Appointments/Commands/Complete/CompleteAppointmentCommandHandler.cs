using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Appointments.Commands.Complete;

public sealed class CompleteAppointmentCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteAppointmentCommand>
{
    public async Task Handle(CompleteAppointmentCommand request, CancellationToken ct)
    {
        // 1. Who is completing: always from the access token
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // 2. Only the dentist of this appointment (IDOR), checked inside the query.
        //    Somebody else's appointment gives the same 404 as a missing one.
        //    Tracked (no AsNoTracking), because the appointment is changed below.
        var appointment = await ctx.Appointments
            .Where(x => x.Id == request.Id && x.Dentist.UserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new MarketNotFoundException("Termin nije pronađen.");

        // 3. Only a scheduled appointment can be completed (not cancelled, not completed twice)
        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new MarketConflictException("Može se završiti samo zakazan termin.");

        // 4. Only after the appointment has started
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (appointment.StartUtc > nowUtc)
            throw new MarketConflictException("Posjeta se može završiti tek kada termin počne.");

        // 5. Current prices of the extra services. Only active services can be added.
        //    Distinct: the same service can be added more times, but is read once.
        var requestedIds = request.AdditionalDentalServiceIds.Distinct().ToList();

        var prices = await ctx.DentalServices
            .AsNoTracking()
            .Where(x => requestedIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, x => x.PriceBam, ct);

        if (prices.Count != requestedIds.Count)
            throw new MarketConflictException("Neka od izabranih usluga više nije dostupna. Osvježite listu usluga.");

        // 6. One new row per performed procedure. The price is copied,
        //    so later price changes don't affect this appointment.
        //    The service chosen at booking stays as it is.
        foreach (var serviceId in request.AdditionalDentalServiceIds)
        {
            appointment.Services.Add(new AppointmentServiceEntity
            {
                DentalServiceId = serviceId,
                PriceAtTimeBam = prices[serviceId]
            });
        }

        // 7. Status, time and the medical record
        appointment.Status = AppointmentStatus.Completed;
        appointment.CompletedAtUtc = nowUtc;
        appointment.MedicalRecord = new MedicalRecordEntity
        {
            Findings = request.Findings.Trim(),
            Therapy = string.IsNullOrWhiteSpace(request.Therapy) ? null : request.Therapy.Trim()
        };

        // 8. Appointment, new services and the record in one SaveChangesAsync = one transaction
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index on MedicalRecords.AppointmentId:
            // the same appointment was completed at the same moment by another request.
            throw new MarketConflictException("Termin je već završen.");
        }
    }
}
