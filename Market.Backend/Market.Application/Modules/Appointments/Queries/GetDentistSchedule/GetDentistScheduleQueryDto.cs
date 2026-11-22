using Market.Domain.Entities.Appointments;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Queries.GetDentistSchedule;
/// <summary>
/// One day of the dentist's schedule. Empty WorkingHours = the dentist doesn't work that day.
/// </summary>
public sealed class GetDentistScheduleQueryDto
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<DentistScheduleWorkingHoursDto> WorkingHours { get; init;}
    public required IReadOnlyList<DentistScheduleAppointmentDto> Appointments { get; init;}

}

/// <summary>One working interval, in local clinic time (not UTC), e.g. 08:00-16:00.</summary>
public sealed class DentistScheduleWorkingHoursDto
{
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
}

/// <summary>One appointment in the schedule. Times are UTC; the frontend shows them in clinic time.</summary>
public sealed class DentistScheduleAppointmentDto
{
    public required int Id { get; init; }
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required AppointmentStatus Status { get; init; }

    public required string PatientFullName { get; init; }

    /// <summary>Null if the patient hasn't entered a phone in the profile.</summary>
    public string? PatientPhone { get; init; }

    public required IReadOnlyList<string> DentalServiceNames { get; init; }
    public required decimal TotalPriceBam { get; init; }
}
