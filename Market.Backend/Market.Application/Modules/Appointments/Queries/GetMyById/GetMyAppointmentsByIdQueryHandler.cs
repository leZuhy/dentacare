namespace Market.Application.Modules.Appointments.Queries.GetMyById;

public sealed class GetMyAppointmentByIdQueryHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser)
    : IRequestHandler<GetMyAppointmentByIdQuery, GetMyAppointmentByIdQueryDto>
{
    public async Task<GetMyAppointmentByIdQueryDto> Handle(
        GetMyAppointmentByIdQuery request,
        CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // Id AND owner in the same WHERE: somebody else's appointment is simply "not found" (IDOR)
        var dto = await ctx.Appointments
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.Patient.UserId == userId)
            .Select(x => new GetMyAppointmentByIdQueryDto
            {
                Id = x.Id,
                StartUtc = x.StartUtc,
                EndUtc = x.EndUtc,
                Status = x.Status,
                CancelledAtUtc = x.CancelledAtUtc,
                CancelledByDentist = x.CancelledByUserId == x.Dentist.UserId,
                DentistFullName = x.Dentist.User.Firstname + " " + x.Dentist.User.Lastname,
                DentistSpecializationName = x.Dentist.Specialization.Name,
                Services = x.Services
                    .OrderBy(s => s.Id)
                    .Select(s => new GetMyAppointmentByIdQueryServiceDto
                    {
                        Name = s.DentalService.Name,
                        PriceAtTimeBam = s.PriceAtTimeBam
                    })
                    .ToList(),
                TotalPriceBam = x.Services.Sum(s => s.PriceAtTimeBam)
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            throw new MarketNotFoundException("Termin nije pronađen.");

        return dto;
    }
}