namespace Market.Application.Modules.Appointments.Queries.ListMy;

public sealed class ListMyAppointmentsQueryValidator : AbstractValidator<ListMyAppointmentsQuery>
{
    public ListMyAppointmentsQueryValidator()
    {
        RuleFor(x => x.Period)
            .IsInEnum()
            .WithMessage("Period nije ispravan.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status nije ispravan.")
            .When(x => x.Status.HasValue);
    }
}