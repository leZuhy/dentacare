namespace Market.Application.Modules.Public.Queries.ListDentists;

public sealed class ListPublicDentistsQueryValidator : AbstractValidator<ListPublicDentistsQuery>
{
    public ListPublicDentistsQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MarketUserEntity.Constraints.FirstnameMaxLength)
            .WithMessage($"Pretraga može imati najviše {MarketUserEntity.Constraints.FirstnameMaxLength} znakova.");

        RuleFor(x => x.SpecializationId)
            .GreaterThan(0)
            .WithMessage("Specijalizacija nije ispravna.")
            .When(x => x.SpecializationId.HasValue);
    }
}