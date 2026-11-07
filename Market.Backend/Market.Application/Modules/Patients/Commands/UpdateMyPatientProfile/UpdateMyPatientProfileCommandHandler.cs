namespace Market.Application.Modules.Patients.Commands.UpdateMyPatientProfile;

public sealed class UpdateMyPatientProfileCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<UpdateMyPatientProfileCommand>
{
    public async Task Handle(UpdateMyPatientProfileCommand request, CancellationToken ct)
    {
        // Format and limits are checked by UpdateMyPatientProfileCommandValidator.
        // The patient is always the one from the access token: nobody can change another patient here.
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        var patient = await ctx.Patients
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new MarketNotFoundException($"Profil pacijenta (korisnik ID={userId}) nije pronađen.");

        patient.Phone = request.Phone.TrimToNull();
        patient.DateOfBirth = request.DateOfBirth;

        await ctx.SaveChangesAsync(ct);
    }
}