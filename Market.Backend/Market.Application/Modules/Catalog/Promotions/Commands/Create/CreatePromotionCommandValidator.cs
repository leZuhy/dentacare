namespace Market.Application.Modules.Catalog.Promotions.Commands.Create;

public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Naslov je obavezan.")
            .MaximumLength(PromotionEntity.Constraints.TitleMaxLength).WithMessage($"Naslov može imati najviše {PromotionEntity.Constraints.TitleMaxLength} znakova.");

        RuleFor(x => x.ImageUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("URL slike je obavezan.")
            .MaximumLength(PromotionEntity.Constraints.ImageUrlMaxLength).WithMessage($"URL slike može imati najviše {PromotionEntity.Constraints.ImageUrlMaxLength} znakova.")
            .Must(UrlRules.IsAbsoluteHttpUrl).WithMessage("URL slike mora biti apsolutna http(s) adresa.");

        RuleFor(x => x.TargetUrl)
            .MaximumLength(PromotionEntity.Constraints.TargetUrlMaxLength).WithMessage($"Odredišni URL može imati najviše {PromotionEntity.Constraints.TargetUrlMaxLength} znakova.")
            .Must(url => UrlRules.IsAbsoluteHttpUrl(url) || UrlRules.IsRelativeRoute(url))
                .WithMessage("Odredišni URL mora biti apsolutna http(s) adresa ili ruta aplikacije koja počinje sa \"/\".")
            .When(x => !string.IsNullOrWhiteSpace(x.TargetUrl));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Redoslijed ne može biti negativan.");

        RuleFor(x => x.EndsAtUtc)
            .GreaterThan(x => x.StartsAtUtc).WithMessage("Kraj promocije mora biti nakon početka.")
            .When(x => x.StartsAtUtc is not null && x.EndsAtUtc is not null);
    }
}
