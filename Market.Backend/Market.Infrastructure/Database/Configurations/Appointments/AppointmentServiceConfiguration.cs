using Market.Domain.Entities.Appointments;

namespace Market.Infrastructure.Database.Configurations.Appointments;

public sealed class AppointmentServiceConfiguration : IEntityTypeConfiguration<AppointmentServiceEntity>
{
    public void Configure(EntityTypeBuilder<AppointmentServiceEntity> builder)
    {
        builder
            .ToTable("AppointmentServices");

        builder
            .Property(x => x.PriceAtTimeBam)
            .IsRequired()
            .HasPrecision(10, 2);

        builder
            .HasOne(x => x.Appointment)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.DentalService)
            .WithMany()
            .HasForeignKey(x => x.DentalServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}