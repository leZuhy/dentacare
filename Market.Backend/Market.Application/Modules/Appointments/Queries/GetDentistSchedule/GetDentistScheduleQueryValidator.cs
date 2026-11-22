using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Queries.GetDentistSchedule;

public sealed class GetDentistScheduleQueryValidator : AbstractValidator<GetDentistScheduleQuery>
{
    public GetDentistScheduleQueryValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Datum je obavezan.");
    }
}
