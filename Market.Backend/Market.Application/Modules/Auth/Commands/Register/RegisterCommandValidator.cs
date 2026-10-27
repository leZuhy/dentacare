namespace Market.Application.Modules.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Firstname)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(MarketUserEntity.Constraints.FirstnameMaxLength)
            .WithMessage($"Ime može imati najviše {MarketUserEntity.Constraints.FirstnameMaxLength} znakova.");

        RuleFor(x => x.Lastname)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(MarketUserEntity.Constraints.LastnameMaxLength)
            .WithMessage($"Prezime može imati najviše {MarketUserEntity.Constraints.LastnameMaxLength} znakova.");

        // Cascade.Stop: the database check runs only for a valid email.
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email je obavezan.")
            .MaximumLength(MarketUserEntity.Constraints.EmailMaxLength)
            .WithMessage($"Email može imati najviše {MarketUserEntity.Constraints.EmailMaxLength} znakova.")
            .EmailAddress().WithMessage("Email nije ispravan.")
            .MustAsync(async (email, ct) =>
            {
                // Case-insensitive, like login; soft-deleted users are skipped by the global query filter
                // (the unique index on Email also ignores them).
                var normalized = email.Trim().ToLower();
                return !await ctx.Users.AnyAsync(u => u.Email.ToLower() == normalized, ct);
            })
            .WithMessage("Email je već registrovan.");

        RuleFor(x => x.Password)
            .ValidPassword();

        RuleFor(x => x.Address)
            .MaximumLength(MarketUserEntity.Constraints.AddressMaxLength)
            .WithMessage($"Adresa može imati najviše {MarketUserEntity.Constraints.AddressMaxLength} znakova.");

        RuleFor(x => x.City)
            .MaximumLength(MarketUserEntity.Constraints.CityMaxLength)
            .WithMessage($"Grad može imati najviše {MarketUserEntity.Constraints.CityMaxLength} znakova.");
    }
}
