namespace Market.Application.Modules.Appointments.Commands.Create;

/// <summary>
/// Checks only the format of the request (400).
/// Business rules (is the slot free, 60 days ahead, overlap) are in the handler (404 / 409),
/// because they need the database and the current time.
/// </summary>
public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.DentistId)
            .GreaterThan(0)
            .WithMessage("Stomatolog nije ispravan.");

        RuleFor(x => x.DentalServiceId)
            .GreaterThan(0)
            .WithMessage("Usluga nije ispravna.");

        RuleFor(x => x.StartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Početak termina je obavezan.")
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Početak termina mora biti u UTC vremenu (završava sa \"Z\").");
    }
}