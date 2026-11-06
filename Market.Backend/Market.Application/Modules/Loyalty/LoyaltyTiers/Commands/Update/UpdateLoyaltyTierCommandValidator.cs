using Market.Domain.Entities.Loyalty;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Update;

public sealed class UpdateLoyaltyTierCommandValidator : AbstractValidator<UpdateLoyaltyTierCommand>
{
    public UpdateLoyaltyTierCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Naziv je obavezan")
            .MaximumLength(LoyaltyTierEntity.Constraints.NameMaxLength)
            .WithMessage($"Naziv može imati najviše {LoyaltyTierEntity.Constraints.NameMaxLength} znakova");

        RuleFor(x => x.Name)
            .MustAsync(async (command, name, ct) => 
                !await ctx.LoyaltyTiers.AnyAsync(x =>
                x.Name == command.Name.Trim() &&
                x.Id != command.Id ,ct))
            .WithMessage("Nivo s ovim nazivom već postoji")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.MinPoints)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimalni broj bodova mora biti pozitivan");

        RuleFor(x => x.MinPoints)
            .MustAsync(async (command, minPoints, ct) =>
                !await ctx.LoyaltyTiers.AnyAsync(x =>
                    x.Id != command.Id &&
                    x.MinPoints == minPoints, ct))
            .WithMessage("Nivo s ovim brojem bodova već postoji");

        RuleFor(x => x.MinPoints)
            .MustAsync(async (command, minPoints, ct) =>
            {
                var currentMinPoints = await ctx.LoyaltyTiers
                    .Where(x => x.Id == command.Id)
                    .Select(x => (int?)x.MinPoints)
                    .FirstOrDefaultAsync(ct);

                if (currentMinPoints is null)
                    return true;

                return currentMinPoints != LoyaltyTierEntity.Constraints.BaseTierMinPoints
                    || minPoints == LoyaltyTierEntity.Constraints.BaseTierMinPoints;
            })
            .WithMessage("Prvom nivou broj bodova mora biti 0");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, LoyaltyTierEntity.Constraints.DiscountPercentMax)
            .WithMessage($"Popust mora biti između 0 i {LoyaltyTierEntity.Constraints.DiscountPercentMax} %")
            .PrecisionScale(5, 2, true)
            .WithMessage("Popust može imati najviše 2 decimale");

        RuleFor(x => x.DiscountPercent)
            .MustAsync(async (command, discount, ct) =>
                !await ctx.LoyaltyTiers.AnyAsync(x =>
                    x.Id != command.Id &&
                    ((x.MinPoints < command.MinPoints && x.DiscountPercent >= discount) ||
                    (x.MinPoints > command.MinPoints && x.DiscountPercent <= discount)), ct))
            .WithMessage("Popust mora biti veci nego kod nizih nivoa i manji nego kod visih nivoa")
            .When(x => x.DiscountPercent >= 0 && x.DiscountPercent <= LoyaltyTierEntity.Constraints.DiscountPercentMax);
    }
}
