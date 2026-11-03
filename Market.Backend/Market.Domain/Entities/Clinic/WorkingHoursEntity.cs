using Market.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Domain.Entities.Clinic
{
    public sealed class WorkingHoursEntity : BaseEntity
    {
        public int DentistId { get; set; }
        public DentistEntity Dentist { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
