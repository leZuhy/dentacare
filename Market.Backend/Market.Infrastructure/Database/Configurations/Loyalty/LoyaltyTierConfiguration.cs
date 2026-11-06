using Market.Domain.Entities.Loyalty;

namespace Market.Infrastructure.Database.Configurations.Loyalty;

public sealed class LoyaltyTierConfiguration : IEntityTypeConfiguration<LoyaltyTierEntity>
{
    public void Configure(EntityTypeBuilder<LoyaltyTierEntity> builder)
    {
        builder
            .ToTable("LoyaltyTiers");


        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(LoyaltyTierEntity.Constraints.NameMaxLength);

        builder
            .Property(x => x.DiscountPercent)
            .HasPrecision(5, 2);

        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder
            .HasIndex(x => x.MinPoints)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
