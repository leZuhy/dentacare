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
