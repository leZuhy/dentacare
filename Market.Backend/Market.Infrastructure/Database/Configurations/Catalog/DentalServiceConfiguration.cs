using Market.Domain.Entities.Catalog;

namespace Market.Infrastructure.Database.Configurations.Catalog;

public sealed class DentalServiceConfiguration : IEntityTypeConfiguration<DentalServiceEntity>
{
    public void Configure(EntityTypeBuilder<DentalServiceEntity> builder)
    {
        builder
            .ToTable("DentalServices");
        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(DentalServiceEntity.Constraints.NameMaxLength);
        builder
            .Property(x => x.Description)
            .HasMaxLength(DentalServiceEntity.Constraints.DescriptionMaxLength);
        builder
            .Property(x => x.PriceBam)
            .IsRequired()
            .HasPrecision(10, 2);
        builder
            .HasOne(x => x.Category)
            .WithMany(x => x.DentalServices)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
