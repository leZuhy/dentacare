using Market.Application.Modules.Appointments.Common;

namespace Market.Application.Modules.Appointments.Queries.ListRescheduleFreeSlots;

public sealed class ListRescheduleFreeSlotsQueryValidator : AbstractValidator<ListRescheduleFreeSlotsQuery>
{
    public ListRescheduleFreeSlotsQueryValidator()
    {
        // default(DateOnly) = 0001-01-01 means the date was not sent
        RuleFor(x => x.FromDate)
            .NotEmpty()
            .WithMessage("Početni datum je obavezan.");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Krajnji datum ne može biti prije početnog.")
            .When(x => x.ToDate.HasValue);

        // both dates are included: 1st to 14th is 14 days (difference 13)
        RuleFor(x => x.ToDate)
            .Must((query, toDate) => toDate!.Value.DayNumber - query.FromDate.DayNumber < FreeSlotCalculator.MaxRangeDays)
            .WithMessage($"Raspon može biti najviše {FreeSlotCalculator.MaxRangeDays} dana.")
            .When(x => x.ToDate.HasValue);
    }
}