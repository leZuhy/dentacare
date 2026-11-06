using Market.Domain.Entities.Loyalty;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Infrastructure.Database.Configurations.Loyalty;

public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccountEntity>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccountEntity> builder)
    {
        builder
            .ToTable("LoyaltyAccounts");

        builder
            .HasOne(x => x.Patient)
            .WithOne(p => p.LoyaltyAccount)
            .HasForeignKey<LoyaltyAccountEntity>(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.PatientId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder
            .HasOne(x => x.Tier)
            .WithMany()
            .HasForeignKey(x => x.TierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
