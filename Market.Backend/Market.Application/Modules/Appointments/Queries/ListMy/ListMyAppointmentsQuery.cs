using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Appointments.Queries.ListMy;

/// <summary>
/// Appointments of the logged-in patient, with paging and filters.
/// There is no PatientId: the patient is always taken from the access token (IDOR).
/// Query string: ?period=upcoming&amp;status=scheduled&amp;paging.page=1&amp;paging.pageSize=10
/// </summary>
public sealed class ListMyAppointmentsQuery : BasePagedQuery<ListMyAppointmentsQueryDto>
{
    /// <summary>Upcoming (default), Past or All.</summary>
    public AppointmentPeriod Period { get; init; } = AppointmentPeriod.Upcoming;

    /// <summary>Only appointments with this status. (optional)</summary>
    public AppointmentStatus? Status { get; init; }
}