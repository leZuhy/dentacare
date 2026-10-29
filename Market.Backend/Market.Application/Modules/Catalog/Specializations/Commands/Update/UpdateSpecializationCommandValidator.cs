using Market.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Update
{
    public sealed class UpdateSpecializationCommandValidator : AbstractValidator<UpdateSpecializationCommand>
    {
        public UpdateSpecializationCommandValidator(IAppDbContext ctx)
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Naziv je obavezan")
                .MaximumLength(SpecializationEntity.Constraints.NameMaxLength)
                .MustAsync(async(command, name, ct) =>
                {
                    var normalized = name.Trim().ToLower();
                    return !await ctx.Specializations.AnyAsync(x => x.Id != command.Id && x.Name.ToLower() == normalized,ct);
                })
                .WithMessage("Specijalizacija sa tim nazivom već postoji.");
        }
    }
}
