namespace Market.Application.Modules.Patients.Queries.GetMyPatientProfile;

public sealed class GetMyPatientProfileQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetMyPatientProfileQuery, GetMyPatientProfileQueryDto>
{
    public async Task<GetMyPatientProfileQueryDto> Handle(GetMyPatientProfileQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen");

        var dto = await ctx.Patients
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new GetMyPatientProfileQueryDto
            {
                Phone = x.Phone,
                DateOfBirth = x.DateOfBirth
            })
            .FirstOrDefaultAsync(ct);

        return dto ?? throw new MarketNotFoundException($"Profil pacijenta sa Id-em {userId} ne postoji");
    }
}
