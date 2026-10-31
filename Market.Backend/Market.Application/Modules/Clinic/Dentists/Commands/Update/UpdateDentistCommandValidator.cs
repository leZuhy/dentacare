using Market.Domain.Entities.Clinic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Update
{
    public sealed class UpdateDentistCommandValidator : AbstractValidator<UpdateDentistCommand>
    {
        public UpdateDentistCommandValidator(IAppDbContext ctx)
        {
            RuleFor(x => x.Firstname)
                .NotEmpty().WithMessage("Ime je obavezno.")
                .MaximumLength(MarketUserEntity.Constraints.FirstnameMaxLength)
                .WithMessage($"Ime moze imati najvise {MarketUserEntity.Constraints.FirstnameMaxLength} karaktera.");

            RuleFor(x => x.Lastname)
                .NotEmpty().WithMessage("Prezime je obavezno.")
                .MaximumLength(MarketUserEntity.Constraints.LastnameMaxLength)
                .WithMessage($"Prezime moze imati najvise {MarketUserEntity.Constraints.LastnameMaxLength} karaktera.");

            RuleFor(x => x.SpecializationId)
                .MustAsync(async (id, ct) => await ctx.Specializations.AnyAsync(x => x.Id == id, ct))
                .WithMessage("Izabrana specijalizacija ne postoji.");

            RuleFor(x => x.Bio)
                .MaximumLength(DentistEntity.Constraints.BioMaxLength)
                .WithMessage($"Biografija moze imati najvise {DentistEntity.Constraints.BioMaxLength} karaktera.");
        }
    }
}
