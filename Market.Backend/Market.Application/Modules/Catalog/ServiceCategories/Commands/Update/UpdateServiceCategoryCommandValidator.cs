using Market.Domain.Entities.Catalog;

namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Update;

public sealed class UpdateServiceCategoryCommandValidator : AbstractValidator<UpdateServiceCategoryCommand>
{
    public UpdateServiceCategoryCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Naziv kategorije je obavezan.")
            .MaximumLength(ServiceCategoryEntity.Constraints.NameMaxLength)
            .WithMessage($"Naziv kategorije ne smije imati više od {ServiceCategoryEntity.Constraints.NameMaxLength} karaktera.")
            .MustAsync(async (command, name, ct) =>
            {
                var normalized = name.Trim().ToLower();
                return !await ctx.ServiceCategories.AnyAsync(x => x.Id != command.Id && x.Name.ToLower() == normalized, ct);
            })
            .WithMessage("Kategorija sa tim nazivom već postoji.");
    }
}
