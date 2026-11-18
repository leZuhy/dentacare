namespace Market.Application.Modules.Appointments.Commands.Reschedule;

/// <summary>
/// Only the format of the request (400). Business rules are in the handler (404 / 409).
/// </summary>
public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.StartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Novi početak termina je obavezan.")
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Novi početak termina mora biti u UTC vremenu (završava sa \"Z\").");
    }
}