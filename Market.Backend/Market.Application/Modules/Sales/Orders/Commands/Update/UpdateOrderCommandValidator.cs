namespace Market.Application.Modules.Sales.Orders.Commands.Update;

public sealed class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Narudžba mora imati bar jednu stavku.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Isti proizvod se ne može pojaviti više puta u narudžbi.")
            .When(x => x.Items is not null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            // Id = 0 means a new item; Id > 0 updates an existing item of this order.
            item.RuleFor(i => i.Id)
                .GreaterThanOrEqualTo(0).WithMessage("Id stavke ne može biti negativan.");

            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("Proizvod je obavezan.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Količina mora biti veća od 0.")
                .Must(q => q == decimal.Truncate(q)).WithMessage("Količina mora biti cijeli broj.");
        });
    }
}
