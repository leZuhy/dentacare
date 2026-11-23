using Market.Domain.Entities.Appointments;

namespace Market.Infrastructure.Database.Configurations.Appointments;

public sealed class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecordEntity>
{
    public void Configure(EntityTypeBuilder<MedicalRecordEntity> builder)
    {
        builder
            .ToTable("MedicalRecords");

        builder
            .Property(x => x.Findings)
            .IsRequired()
            .HasMaxLength(MedicalRecordEntity.Constraints.FindingsMaxLength);

        builder
            .Property(x => x.Therapy)
            .HasMaxLength(MedicalRecordEntity.Constraints.TherapyMaxLength);

        builder
            .HasOne(x => x.Appointment)
            .WithOne(x => x.MedicalRecord)
            .HasForeignKey<MedicalRecordEntity>(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 - one appointment has at most one (not deleted) medical record.
        // Also protects against two completions of the same appointment at the same moment.
        builder
            .HasIndex(x => x.AppointmentId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
