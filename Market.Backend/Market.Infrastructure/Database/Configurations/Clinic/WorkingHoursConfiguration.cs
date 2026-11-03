using Market.Domain.Entities.Clinic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Infrastructure.Database.Configurations.Clinic
{
    public sealed class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHoursEntity>
    {
        public void Configure(EntityTypeBuilder<WorkingHoursEntity> builder)
        {
            builder.
                ToTable("WorkingHours");

            builder
                .HasOne(x => x.Dentist)
                .WithMany(x => x.WorkingHours)
                .HasForeignKey(x => x.DentistId)
                .OnDelete(DeleteBehavior.Restrict);

            //No unique constraint on DentistId and DayOfWeek to allow multiple working hours for the same day.

            builder
                .HasIndex(x => new { x.DentistId, x.DayOfWeek })
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
