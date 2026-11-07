using Market.Domain.Entities.Patients;

namespace Market.Application.Modules.Patients.Commands.UpdateMyPatientProfile;

public sealed class UpdateMyPatientProfileCommandValidator : AbstractValidator<UpdateMyPatientProfileCommand>
{
    public UpdateMyPatientProfileCommandValidator()
    {
        RuleFor(x => x.Phone)
            .MaximumLength(PatientEntity.Constraints.PhoneMaxLength)
            .WithMessage($"Telefon može imati najviše {PatientEntity.Constraints.PhoneMaxLength} znakova.")
            .Matches(@"^\s*\+?[0-9 ()/-]+$")
            .WithMessage("Telefon smije sadržavati samo cifre, razmake i znakove + ( ) / -, a + samo na početku.")
            .Must(HaveValidDigitCount)
            .WithMessage($"Telefon mora imati od {PatientEntity.Constraints.PhoneMinDigits} do {PatientEntity.Constraints.PhoneMaxDigits} cifara.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Must(d => d <= Today())
            .WithMessage("Datum rođenja ne može biti u budućnosti.")
            .Must(d => d >= Today().AddYears(-PatientEntity.Constraints.MaxAgeYears))
            .WithMessage($"Datum rođenja ne može biti stariji od {PatientEntity.Constraints.MaxAgeYears} godina.")
            .When(x => x.DateOfBirth.HasValue);
    }

    private static bool HaveValidDigitCount(string? phone)
    {
        var digits = phone!.Count(char.IsAsciiDigit);
        return digits >= PatientEntity.Constraints.PhoneMinDigits
            && digits <= PatientEntity.Constraints.PhoneMaxDigits;
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}