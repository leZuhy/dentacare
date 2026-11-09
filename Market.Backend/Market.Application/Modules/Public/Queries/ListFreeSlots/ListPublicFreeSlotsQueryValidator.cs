using Market.Application.Modules.Appointments.Common;

namespace Market.Application.Modules.Public.Queries.ListFreeSlots;

/// <summary>
/// A date in the past is not an error: the calculator then returns an empty list.
/// </summary>
public sealed class ListPublicFreeSlotsQueryValidator : AbstractValidator<ListPublicFreeSlotsQuery>
{
    public ListPublicFreeSlotsQueryValidator()
    {
        RuleFor(x => x.DentistId)
            .GreaterThan(0)
            .WithMessage("Stomatolog nije ispravan.");

        RuleFor(x => x.DentalServiceId)
            .GreaterThan(0)
            .WithMessage("Usluga nije ispravna.");

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
