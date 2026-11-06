using Market.Application.Modules.Auth.Common;
using Market.Domain.Entities.Loyalty;
using Market.Domain.Entities.Patients;

namespace Market.Application.Modules.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IAppDbContext ctx,
    IJwtTokenService jwt,
    IPasswordHasher<MarketUserEntity> hasher)
    : IRequestHandler<RegisterCommand, RegisterCommandDto>
{
    public async Task<RegisterCommandDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        // Required fields, lengths, password rule and the unique email are checked by RegisterCommandValidator.

        // The base tier is found by MinPoints, not by name (an admin may rename "Bronze").
        // It always exists: DeleteLoyaltyTier and UpdateLoyaltyTier protect it.
        var baseTier = await ctx.LoyaltyTiers
            .FirstOrDefaultAsync(t => t.MinPoints == LoyaltyTierEntity.Constraints.BaseTierMinPoints, ct)
            ?? throw new InvalidOperationException("Početni loyalty nivo (0 bodova) ne postoji.");

        // A new account is always a patient: roles cannot be chosen during registration.
        var user = new MarketUserEntity
        {
            Firstname = request.Firstname.Trim(),
            Lastname = request.Lastname.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Address = request.Address.TrimToNull(),
            City = request.City.TrimToNull(),
            Role = Role.Patient,
            IsEnabled = true
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        // Phone and date of birth are entered later, in the patient profile (#9).
        var patient = new PatientEntity
        {
            User = user
        };

        var loyaltyAccount = new LoyaltyAccountEntity
        {
            Patient = patient,
            TierId = baseTier.Id,
            PointsBalance = 0,
            LifetimePoints = 0
        };

        ctx.Users.Add(user);
        ctx.Patients.Add(patient);
        ctx.LoyaltyAccounts.Add(loyaltyAccount);

        // One SaveChangesAsync = one database transaction: user, patient and loyalty account are saved
        // together or not at all. It is also needed before the token, because the token contains user.Id.
        await ctx.SaveChangesAsync(ct);

        // The user is logged in right away, like after login
        var tokens = ctx.StartSession(jwt, user);
        await ctx.SaveChangesAsync(ct);

        return new RegisterCommandDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshTokenRaw,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
        };
    }
}
