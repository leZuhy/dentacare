using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Appointments.Queries.ListMy;

/// <summary>
/// "My appointments": only the logged-in patient's appointments (IDOR check inside the query),
/// filtered by period and status, upcoming sorted from the nearest, past from the latest.
/// </summary>
public sealed class ListMyAppointmentsQueryHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ListMyAppointmentsQuery, PageResult<ListMyAppointmentsQueryDto>>
{
    public async Task<PageResult<ListMyAppointmentsQueryDto>> Handle(ListMyAppointmentsQuery request, CancellationToken ct)
    {
        // 1. who is asking: always from the token, never from the request
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // 2. only this patient's appointments; the check is in the query itself (IDOR)
        var q = ctx.Appointments
            .AsNoTracking()
            .Where(x => x.Patient.UserId == userId);

        // 3. status filter (optional)
        if (request.Status is not null)
            q = q.Where(x => x.Status == request.Status);


        // 4. period: upcoming = start in the future; sort depends on the period
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var sorted = request.Period switch
        {
            AppointmentPeriod.Upcoming => q
            .Where(x => x.StartUtc > nowUtc)
            .OrderBy(x => x.StartUtc),
            AppointmentPeriod.Past => q
            .Where(x => x.StartUtc <= nowUtc)
            .OrderByDescending(x => x.StartUtc),
            _ => q.OrderByDescending(x => x.StartUtc)
        };

        // 5. only the columns the list needs, then one page
        var projectedQuery = sorted
            .ThenBy(x => x.Id)
            .Select(x => new ListMyAppointmentsQueryDto
            {
                Id = x.Id,
                StartUtc = x.StartUtc,
                EndUtc = x.EndUtc,
                Status = x.Status,
                DentistFullName = x.Dentist.User.Firstname + " " + x.Dentist.User.Lastname,
                DentalServiceNames = x.Services
                .OrderBy(s => s.Id)
                .Select(s => s.DentalService.Name)
                .ToList(),
                TotalPriceBam = x.Services.Sum(s => s.PriceAtTimeBam)
            });

       return await PageResult<ListMyAppointmentsQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }

}
