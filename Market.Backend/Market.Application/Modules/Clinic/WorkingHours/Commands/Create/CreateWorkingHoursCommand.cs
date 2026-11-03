using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Create
{
    public sealed class CreateWorkingHoursCommand :  IRequest<int>
    {
        public int DentistId { get; init; }
        public DayOfWeek DayOfWeek { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }
}
