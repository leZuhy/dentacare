using Market.Domain.Entities.Clinic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Create
{
    public sealed class CreateDentistCommandValidator : AbstractValidator<CreateDentistCommand>
    {
        public CreateDentistCommandValidator(IAppDbContext ctx)
        {
            RuleFor(x => x.Firstname)
                .NotEmpty().WithMessage("Ime je obavezno")
                .MaximumLength(MarketUserEntity.Constraints.FirstnameMaxLength)
                .WithMessage($"Ime ne smije biti duže od {MarketUserEntity.Constraints.FirstnameMaxLength} karaktera");

            RuleFor(x => x.Lastname)
                .NotEmpty().WithMessage("Prezime je obavezno")
                .MaximumLength(MarketUserEntity.Constraints.LastnameMaxLength)
                .WithMessage($"Prezime ne smije biti duže od {MarketUserEntity.Constraints.LastnameMaxLength} karaktera");

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Email je obavezan")
                .MaximumLength(MarketUserEntity.Constraints.EmailMaxLength)
                .WithMessage($"Email ne smije biti duži od {MarketUserEntity.Constraints.EmailMaxLength} karaktera")
                .EmailAddress().WithMessage("Email nije validan")
                .MustAsync(async (email, ct) =>
                {
                    var normalized = email.Trim().ToLower();
                    return !await ctx.Users.AnyAsync(x => x.Email == normalized, ct);
                })
                .WithMessage("Email već postoji");

            RuleFor(x => x.Password)
                .ValidPassword();

            RuleFor(x => x.SpecializationId)
                .MustAsync(async (id,ct) =>
                {
                    return await ctx.Specializations.AnyAsync(x => x.Id == id, ct);
                })
                .WithMessage("Specijalizacija ne postoji");

            RuleFor(x => x.Bio)
                .MaximumLength(DentistEntity.Constraints.BioMaxLength)
                .WithMessage($"Bio ne smije biti duži od {DentistEntity.Constraints.BioMaxLength} karaktera");
        }
    }
}
