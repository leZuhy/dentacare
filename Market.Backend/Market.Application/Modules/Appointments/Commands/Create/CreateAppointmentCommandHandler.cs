using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Commands.Create;

public sealed class CreateAppointmentCommandHandler(IAppDbContext ctx,IAppCurrentUser currentUser,
    AppointmentSlotChecker slotChecker) 
    : IRequestHandler<CreateAppointmentCommand,CreateAppointmentCommandDto>
{
    public async Task<CreateAppointmentCommandDto> Handle(CreateAppointmentCommand request,CancellationToken ct)
    {
        // 1. Patient is always the one from the access token, never from the request (IDOR)
        var userId = currentUser.UserId ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        var patientId = await ctx.Patients
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new MarketNotFoundException("Profil pacijenta nije pronaden.");


        // 2. Service must exist and be active (name, price and duration are needed)
        var service = await ctx.DentalServices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.DentalServiceId && x.IsActive, ct)
            ?? throw new MarketNotFoundException("Usluga nije pronadena.");


        // 3. Dentist must exist and have an enabled account.
        //    Tracked (no AsNoTracking), because ScheduleVersion is changed before saving.
        var dentist = await ctx.Dentists
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == request.DentistId && x.User.IsEnabled, ct)
            ?? throw new MarketNotFoundException("Stomatolog nije pronaden");


        // 4. Max days ahead, patient not busy, slot still free (shared with rescheduling, #58)
        var endUtc = await slotChecker.EnsureCanTakeAsync(patientId, dentist.Id, request.StartUtc,
            service.DurationMinutes, excludeAppointmentId: null, ct);


        // 8. New appointment with the one chosen service.
        //    The price is copied, so later price changes don't affect this appointment.
        var appointment = new AppointmentEntity
        {
            PatientId = patientId,
            DentistId = dentist.Id,
            StartUtc = request.StartUtc,
            EndUtc = endUtc,
            Status = AppointmentStatus.Scheduled,
            Services = new List<AppointmentServiceEntity>
            {
                new AppointmentServiceEntity
                {
                    DentalServiceId = service.Id,
                    PriceAtTimeBam = service.PriceBam
                }
            }
        };

        ctx.Appointments.Add(appointment);

        // 9. New schedule version. If another booking for this dentist was saved
        //    after we read him, the UPDATE finds no row and nothing is saved (optimistic concurrency).
        dentist.ScheduleVersion = Guid.NewGuid();

        // 10. Appointment, its service and the new version in one SaveChangesAsync = one database transaction
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // DbUpdateConcurrencyException: another booking for this dentist at the same moment.
            // Unique index (DentistId, StartUtc): the same slot was just taken.
            throw new MarketConflictException("Termin je upravo zauzet. Izaberite drugi.");
        }

        // 11. Id is known only after saving (the database gives it)
        return new CreateAppointmentCommandDto
        {
            Id = appointment.Id,
            StartUtc = appointment.StartUtc,
            EndUtc = appointment.EndUtc,
            DentistFullName = $"{dentist.User.Firstname} {dentist.User.Lastname}",
            DentalServiceName = service.Name,
            PriceBam = service.PriceBam
        };
    }
}
