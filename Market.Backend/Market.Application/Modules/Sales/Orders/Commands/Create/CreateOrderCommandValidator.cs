namespace Market.Application.Modules.Sales.Orders.Commands.Create;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Narudžba mora imati bar jednu stavku.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Isti proizvod se ne može pojaviti više puta u narudžbi.")
            .When(x => x.Items is not null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("Proizvod je obavezan.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Količina mora biti veća od 0.")
                .Must(q => q == decimal.Truncate(q)).WithMessage("Količina mora biti cijeli broj.");
        });
    }
}
