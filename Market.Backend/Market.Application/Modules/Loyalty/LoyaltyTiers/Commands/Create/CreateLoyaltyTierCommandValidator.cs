using Market.Domain.Entities.Loyalty;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Create;

public sealed class CreateLoyaltyTierCommandValidator : AbstractValidator<CreateLoyaltyTierCommand>
{
    public CreateLoyaltyTierCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Naziv je obavezan")
            .MaximumLength(LoyaltyTierEntity.Constraints.NameMaxLength)
            .WithMessage($"Naziv može imati najviše {LoyaltyTierEntity.Constraints.NameMaxLength} znakova");

        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) =>
                !await ctx.LoyaltyTiers.AnyAsync(x => x.Name == name.Trim(), ct))
            .WithMessage("Nivo s ovim nazivom već postoji")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.MinPoints)
            .InclusiveBetween(0, LoyaltyTierEntity.Constraints.MinPointsMax)
            .WithMessage($"Broj bodova mora biti između 0 i {LoyaltyTierEntity.Constraints.MinPointsMax}");

        RuleFor(x => x.MinPoints)
            .MustAsync(async (minPoints, ct) =>
                !await ctx.LoyaltyTiers.AnyAsync(x => x.MinPoints == minPoints, ct))
            .WithMessage("Nivo s ovim brojem bodova već postoji");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, LoyaltyTierEntity.Constraints.DiscountPercentMax)
            .WithMessage($"Popust mora biti između 0 i {LoyaltyTierEntity.Constraints.DiscountPercentMax} %")
            .PrecisionScale(5, 2, true)
            .WithMessage("Popust može imati najviše 2 decimale");

        RuleFor(x => x.DiscountPercent)
            .MustAsync(async (command, discount, ct) =>
                !await ctx.LoyaltyTiers.AnyAsync(x =>
                    (x.MinPoints < command.MinPoints && x.DiscountPercent >= discount) ||
                    (x.MinPoints > command.MinPoints && x.DiscountPercent <= discount), ct))
            .WithMessage("Popust mora biti veći nego kod nižih nivoa i manji nego kod viših nivoa")
            .When(x => x.DiscountPercent >= 0 && x.DiscountPercent <= LoyaltyTierEntity.Constraints.DiscountPercentMax);
    }
}
