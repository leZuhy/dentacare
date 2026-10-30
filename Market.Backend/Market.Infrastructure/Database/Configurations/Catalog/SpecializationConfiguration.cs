using System;
using System.Collections.Generic;
using System.Text;
using Market.Domain.Entities.Catalog;

namespace Market.Infrastructure.Database.Configurations.Catalog
{

    //Dentist specialization configuration
    public sealed class SpecializationConfiguration : IEntityTypeConfiguration<SpecializationEntity>
    { 
        public void Configure(EntityTypeBuilder<SpecializationEntity> builder)
        {
            builder
                    .ToTable("Specializations");

            builder
                    .Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(SpecializationEntity.Constraints.NameMaxLength);

            //Unique only among non deleted rows, deleted names can be reused
            builder
                    .HasIndex(x => x.Name)
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
        }

    }
}
