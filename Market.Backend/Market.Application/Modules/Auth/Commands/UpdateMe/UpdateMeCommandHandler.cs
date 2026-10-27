namespace Market.Application.Modules.Auth.Commands.UpdateMe;

public sealed class UpdateMeCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<UpdateMeCommand>
{
    public async Task Handle(UpdateMeCommand request, CancellationToken ct)
    {
        // Required fields and lengths are checked by UpdateMeCommandValidator.
        // The user is always the one from the access token: nobody can change another user's profile here.
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        var user = await ctx.Users
            .FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new MarketNotFoundException($"Korisnik (ID={userId}) nije pronađen.");

        user.Firstname = request.Firstname.Trim();
        user.Lastname = request.Lastname.Trim();
        user.Address = request.Address.TrimToNull();
        user.City = request.City.TrimToNull();

        await ctx.SaveChangesAsync(ct);
    }
}
