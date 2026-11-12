using Market.Domain.Entities.Clinic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Infrastructure.Database.Configurations.Clinic
{
    public sealed class DentistConfiguration : IEntityTypeConfiguration<DentistEntity>
    {
        public void Configure(EntityTypeBuilder<DentistEntity> builder)
        {
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder
                    .ToTable("Dentists");
            builder
                    .Property(x => x.Bio)
                    .HasMaxLength(DentistEntity.Constraints.BioMaxLength);

            // Checked by EF on every UPDATE of a dentist: the row is saved only
            // if ScheduleVersion in the database is still the one we read.
            builder
                    .Property(x => x.ScheduleVersion)
                    .IsConcurrencyToken();

            builder
                    .HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<DentistEntity>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            builder
                    .HasOne(x => x.Specialization)
                    .WithMany()
                    .HasForeignKey(x => x.SpecializationId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
