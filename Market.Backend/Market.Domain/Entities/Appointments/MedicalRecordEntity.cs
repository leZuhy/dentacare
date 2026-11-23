using Market.Domain.Common;

namespace Market.Domain.Entities.Appointments;

/// <summary>
/// The dentist's record of one completed visit (1:1 with Appointment).
/// Created together with completing the appointment.
/// </summary>
public sealed class MedicalRecordEntity : BaseEntity
{
    public int AppointmentId { get; set; }
    public AppointmentEntity Appointment { get; set; } = null!;

    /// <summary>What the dentist found (required).</summary>
    public string Findings { get; set; } = null!;

    /// <summary>Recommended or performed therapy (optional).</summary>
    public string? Therapy { get; set; }

    public static class Constraints
    {
        public const int FindingsMaxLength = 2000;
        public const int TherapyMaxLength = 2000;

        /// <summary>How many extra performed services can be added in one completion.</summary>
        public const int AdditionalServicesMax = 20;
    }
}
