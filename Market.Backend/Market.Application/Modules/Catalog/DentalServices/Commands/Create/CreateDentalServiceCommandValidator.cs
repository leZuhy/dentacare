using Market.Domain.Entities.Catalog;

namespace Market.Application.Modules.Catalog.DentalServices.Commands.Create;

public sealed class CreateDentalServiceCommandValidator : AbstractValidator<CreateDentalServiceCommand>
{
    public CreateDentalServiceCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Naziv je obavezan")
            .MaximumLength(DentalServiceEntity.Constraints.NameMaxLength)
            .WithMessage($"Naziv može imati najviše {DentalServiceEntity.Constraints.NameMaxLength} znakova");

        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) =>
                !await ctx.DentalServices.AnyAsync(x => x.Name == name.Trim(), ct))
            .WithMessage("Usluga s ovim nazivom već postoji")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(DentalServiceEntity.Constraints.DescriptionMaxLength)
            .WithMessage($"Opis može imati najviše {DentalServiceEntity.Constraints.DescriptionMaxLength} znakova");

        RuleFor(x => x.PriceBam)
            .GreaterThan(0)
            .WithMessage("Cijena mora biti veća od 0 KM")
            .LessThanOrEqualTo(DentalServiceEntity.Constraints.PriceMax)
            .WithMessage($"Cijena može biti najviše {DentalServiceEntity.Constraints.PriceMax} KM")
            .PrecisionScale(10, 2, true)
            .WithMessage("Cijena može imati najviše 2 decimale");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(DentalServiceEntity.Constraints.DurationMin, DentalServiceEntity.Constraints.DurationMax)
            .WithMessage($"Trajanje mora biti između {DentalServiceEntity.Constraints.DurationMin} i {DentalServiceEntity.Constraints.DurationMax} minuta")
            .Must(x => x % DentalServiceEntity.Constraints.DurationStep == 0)
            .WithMessage($"Trajanje mora biti višekratnik od {DentalServiceEntity.Constraints.DurationStep} minuta");

        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, ct) => await ctx.ServiceCategories.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Kategorija ne postoji");
    }
}
