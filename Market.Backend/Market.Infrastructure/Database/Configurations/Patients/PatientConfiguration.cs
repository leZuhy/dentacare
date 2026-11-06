using Market.Domain.Entities.Patients;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Infrastructure.Database.Configurations.Patients;

public sealed class PatientConfiguration : IEntityTypeConfiguration<PatientEntity>
{
    public void Configure(EntityTypeBuilder<PatientEntity> builder)
    {

        builder
            .ToTable("Patients");

        builder
            .Property(x => x.Phone)
            .HasMaxLength(PatientEntity.Constraints.PhoneMaxLength);

        builder
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<PatientEntity>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
