using Market.Domain.Entities.Catalog;

namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Create;

public sealed class CreateServiceCategoryCommandValidator : AbstractValidator<CreateServiceCategoryCommand>
{
    public CreateServiceCategoryCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Naziv kategorije je obavezan.")
            .MaximumLength(ServiceCategoryEntity.Constraints.NameMaxLength)
            .WithMessage($"Naziv kategorije ne smije imati više od {ServiceCategoryEntity.Constraints.NameMaxLength} karaktera.")
            .MustAsync(async (name, ct) =>
            {
                var normalized = name.Trim().ToLower();
                return !await ctx.ServiceCategories.AnyAsync(x => x.Name.ToLower() == normalized, ct);
            })
            .WithMessage("Kategorija sa tim nazivom već postoji.");
    }
}
