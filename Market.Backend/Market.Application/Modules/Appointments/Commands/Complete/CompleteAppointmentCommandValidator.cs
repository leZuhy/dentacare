using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Appointments.Commands.Complete;

/// <summary>
/// Only the format of the request (400).
/// Business rules (own appointment, status, start time, active services) are in the handler (404 / 409).
/// </summary>
public sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    public CompleteAppointmentCommandValidator()
    {
        // NotEmpty on a string also rejects text made only of spaces
        RuleFor(x => x.Findings)
            .NotEmpty()
            .WithMessage("Nalaz je obavezan.")
            .MaximumLength(MedicalRecordEntity.Constraints.FindingsMaxLength)
            .WithMessage($"Nalaz može imati najviše {MedicalRecordEntity.Constraints.FindingsMaxLength} znakova.");

        RuleFor(x => x.Therapy)
            .MaximumLength(MedicalRecordEntity.Constraints.TherapyMaxLength)
            .WithMessage($"Terapija može imati najviše {MedicalRecordEntity.Constraints.TherapyMaxLength} znakova.");

        // "additionalDentalServiceIds": null in JSON -> null list
        RuleFor(x => x.AdditionalDentalServiceIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Lista dodatnih usluga je obavezna (može biti prazna).")
            .Must(x => x.Count <= MedicalRecordEntity.Constraints.AdditionalServicesMax)
            .WithMessage($"Može se dodati najviše {MedicalRecordEntity.Constraints.AdditionalServicesMax} usluga.");

        RuleForEach(x => x.AdditionalDentalServiceIds)
            .GreaterThan(0)
            .WithMessage("Usluga nije ispravna.")
            .When(x => x.AdditionalDentalServiceIds is not null);
    }
}
