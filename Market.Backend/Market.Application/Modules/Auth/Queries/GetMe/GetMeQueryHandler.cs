namespace Market.Application.Modules.Auth.Queries.GetMe;

public sealed class GetMeQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetMeQuery, GetMeQueryDto>
{
    public async Task<GetMeQueryDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        var dto = await ctx.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new GetMeQueryDto
            {
                Id = x.Id,
                Email = x.Email,
                Firstname = x.Firstname,
                Lastname = x.Lastname,
                Address = x.Address,
                City = x.City,
                IsAdmin = x.IsAdmin,
                IsManager = x.IsManager,
                IsEmployee = x.IsEmployee
            })
            .FirstOrDefaultAsync(ct);

        return dto ?? throw new MarketNotFoundException($"Korisnik (ID={userId}) nije pronađen.");
    }
}
