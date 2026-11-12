using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Commands.Create;

public sealed class CreateAppointmentCommandHandler(IAppDbContext ctx,IAppCurrentUser currentUser,
    TimeProvider timeProvider, IOptions<ClinicOptions> clinicOptions) 
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


        // 4. Not more than MaxBookingDaysAhead days ahead (local clinic dates)
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicOptions.Value.TimeZoneId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var startDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(request.StartUtc, timeZone));
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        var lastDate = today.AddDays(clinicOptions.Value.MaxBookingDaysAhead);

        if (startDate > lastDate)
            throw new MarketConflictException($"Termin se moze zakazati najvise {clinicOptions.Value.MaxBookingDaysAhead} dana unaprijed");


        // 5. End of the appointment, from the service duration
        var endUtc = request.StartUtc.AddMinutes(service.DurationMinutes);

        // 6. The patient can't have two active appointments at the same time, with any dentist.
        //    Same overlap rule as everywhere: touching is allowed (9:00-10:00 and 10:00-10:30).
        var patientIsBusy = await ctx.Appointments
            .AnyAsync(x => x.PatientId == patientId
                            && x.Status != AppointmentStatus.Cancelled
                            && x.StartUtc < endUtc
                            && request.StartUtc < x.EndUtc, ct);

        if (patientIsBusy)
            throw new MarketConflictException("Vec imate termin u to vrijeme");


        // 7. Check again that the requested start is still a free slot.
        //    The list the patient saw may be old. Same calculation as GET api/public/free-slots (#12).
        var workingHours = await ctx.WorkingHours
            .AsNoTracking()
            .Where(x => x.DentistId == dentist.Id)
            .ToListAsync(ct);


        // Wide UTC bounds (one extra day on each side), same as in #12
        var rangeStartUtc = DateTime.SpecifyKind(startDate.AddDays(-1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var rangeEndUtc = DateTime.SpecifyKind(startDate.AddDays(2).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);


        var busy = await ctx.Appointments
            .AsNoTracking()
            .Where(x => x.DentistId == dentist.Id
                        && x.Status != AppointmentStatus.Cancelled
                        && x.StartUtc < rangeEndUtc
                        && x.EndUtc > rangeStartUtc)
            .Select(x => new TimeSlot(x.StartUtc, x.EndUtc))
            .ToListAsync(ct);

        var freeSlots = FreeSlotCalculator.Calculate(
            startDate,
            startDate,
            workingHours,
            busy,
            service.DurationMinutes,
            nowUtc,
            timeZone);


        var isFree = freeSlots.Any(x => x.StartUtc == request.StartUtc);

        if (!isFree)
            throw new MarketConflictException("Termin vise nije slobodan. Izaberite drugi");


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
