using Market.Domain.Common;
using Market.Domain.Entities.Clinic;
using Market.Domain.Entities.Patients;

namespace Market.Domain.Entities.Appointments;

/// <summary>
/// A booked visit of a patient to a dentist. Times are stored in UTC.
/// </summary>
public sealed class AppointmentEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;

    public int DentistId { get; set; }
    public DentistEntity Dentist { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<AppointmentServiceEntity> Services { get; set; } = new List<AppointmentServiceEntity>();
}