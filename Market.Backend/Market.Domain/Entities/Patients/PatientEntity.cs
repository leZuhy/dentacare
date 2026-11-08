using Market.Domain.Common;
using Market.Domain.Entities.Identity;
using Market.Domain.Entities.Loyalty;
using Market.Domain.Entities.Appointments;

namespace Market.Domain.Entities.Patients;

/// <summary>
/// Patient profile, 1:1 with a user account (MarketUser with Role.Patient).
/// Created together with the account during registration.
/// </summary>
public sealed class PatientEntity : BaseEntity
{
    public int UserId { get; set; }
    public MarketUserEntity User { get; set; } = null!;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public LoyaltyAccountEntity LoyaltyAccount { get; set; } = null!;
    public ICollection<AppointmentEntity> Appointments { get; set; } = new List<AppointmentEntity>();



    public static class Constraints
    {
        public const int PhoneMaxLength = 20;
        public const int PhoneMinDigits = 8;
        public const int PhoneMaxDigits = 15;
        public const int MaxAgeYears = 120;
    }
}
