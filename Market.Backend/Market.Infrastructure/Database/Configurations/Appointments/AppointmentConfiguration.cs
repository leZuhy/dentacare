using Market.Domain.Entities.Appointments;

namespace Market.Infrastructure.Database.Configurations.Appointments;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<AppointmentEntity>
{
    public void Configure(EntityTypeBuilder<AppointmentEntity> builder)
    {
        builder
            .ToTable("Appointments");

        builder
            .HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Dentist)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.DentistId)
            .OnDelete(DeleteBehavior.Restrict);

        // One dentist can't have two active appointments starting at the same moment.
        // Also used for overlap checks, free slots and the dentist's schedule.
        // 2 = AppointmentStatus.Cancelled (SQL filter needs the number, not the enum name).
        builder
            .HasIndex(x => new { x.DentistId, x.StartUtc })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Status] <> 2");

        // Used for "my appointments" and reminders (next 24 h).
        builder
            .HasIndex(x => new { x.PatientId, x.StartUtc })
            .HasFilter("[IsDeleted] = 0");
    }
}