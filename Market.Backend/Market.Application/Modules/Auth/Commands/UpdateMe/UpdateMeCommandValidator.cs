namespace Market.Application.Modules.Auth.Commands.UpdateMe;

/// <summary>
/// Same rules for these fields as <see cref="Register.RegisterCommandValidator"/>.
/// </summary>
public sealed class UpdateMeCommandValidator : AbstractValidator<UpdateMeCommand>
{
    public UpdateMeCommandValidator()
    {
        RuleFor(x => x.Firstname)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(MarketUserEntity.Constraints.FirstnameMaxLength)
            .WithMessage($"Ime može imati najviše {MarketUserEntity.Constraints.FirstnameMaxLength} znakova.");

        RuleFor(x => x.Lastname)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(MarketUserEntity.Constraints.LastnameMaxLength)
            .WithMessage($"Prezime može imati najviše {MarketUserEntity.Constraints.LastnameMaxLength} znakova.");

        RuleFor(x => x.Address)
            .MaximumLength(MarketUserEntity.Constraints.AddressMaxLength)
            .WithMessage($"Adresa može imati najviše {MarketUserEntity.Constraints.AddressMaxLength} znakova.");

        RuleFor(x => x.City)
            .MaximumLength(MarketUserEntity.Constraints.CityMaxLength)
            .WithMessage($"Grad može imati najviše {MarketUserEntity.Constraints.CityMaxLength} znakova.");
    }
}
