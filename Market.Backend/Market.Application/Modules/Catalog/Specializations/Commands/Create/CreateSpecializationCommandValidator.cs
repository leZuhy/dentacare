using Market.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Create
{
    public sealed class CreateSpecializationCommandValidator : AbstractValidator<CreateSpecializationCommand>
    {
        public CreateSpecializationCommandValidator(IAppDbContext ctx)
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Naziv specijalizacije je obavezan.")
                .MaximumLength(SpecializationEntity.Constraints.NameMaxLength)
                .WithMessage($"Naziv specijalizacije ne smije imati više od {SpecializationEntity.Constraints.NameMaxLength} karaktera.")
                .MustAsync(async (name, ct) =>
                {
                    var normalized = name.Trim().ToLower();
                    return !await ctx.Specializations.AnyAsync(x => x.Name.ToLower() == normalized, ct);
                })
                .WithMessage("Specijalizacija sa tim nazivom već postoji.");
        }
    }
}
