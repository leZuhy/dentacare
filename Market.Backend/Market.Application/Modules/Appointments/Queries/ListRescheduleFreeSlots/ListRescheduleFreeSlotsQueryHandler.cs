using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Appointments;
using Market.Shared.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Queries.ListRescheduleFreeSlots;

public sealed class ListRescheduleFreeSlotsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser, TimeProvider timeProvider,
    IOptions<ClinicOptions> clinicOptions, AppointmentSlotChecker slotChecker) 
    : IRequestHandler<ListRescheduleFreeSlotsQuery,List<ListRescheduleFreeSlotsQueryDto>>
{
    public async Task<List<ListRescheduleFreeSlotsQueryDto>> Handle(ListRescheduleFreeSlotsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        var appointment = await ctx.Appointments
            .Where(x => x.Id == request.Id && x.Patient.UserId == userId)
            .Select(x => new { x.Id, x.DentistId, x.StartUtc, x.EndUtc, x.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new MarketNotFoundException("Termin nije pronaden.");


        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new MarketConflictException("Moze se pomjeriti samo zakazan termin.");


        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var minHours = clinicOptions.Value.CancellationMinHoursBefore;

        if (!AppointmentCancellationPolicy.CanCancel(appointment.StartUtc, nowUtc, minHours))
            throw new MarketConflictException($"Termin se moze pomjeriti najkasnije {minHours} sati.");


        var dentistIsEnabled = await ctx.Dentists
            .AnyAsync(x => x.Id == appointment.DentistId && x.User.IsEnabled, ct);

        if (!dentistIsEnabled)
            throw new MarketConflictException("Stomatolog trenutno ne prima termine");


        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicOptions.Value.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        var lastDate = today.AddDays(clinicOptions.Value.MaxBookingDaysAhead);


        if (request.FromDate > lastDate)
            return new List<ListRescheduleFreeSlotsQueryDto>();

        var toDate = request.ToDate ?? request.FromDate;
        if (toDate > lastDate)
            toDate = lastDate;


        var durationMinutes = (int)(appointment.EndUtc - appointment.StartUtc).TotalMinutes;

        var slots = await slotChecker.GetFreeSlotsAsync(appointment.DentistId, request.FromDate, toDate, durationMinutes, excludeAppointmentId: appointment.Id, ct);


        return slots
             .Where(x => x.StartUtc != appointment.StartUtc)
             .Select(x => new ListRescheduleFreeSlotsQueryDto
             {
                 StartUtc = x.StartUtc,
                 EndUtc = x.EndUtc
             })
             .ToList();
    
    
    }
}
