using Market.Domain.Entities.Catalog;

namespace Market.Infrastructure.Database.Configurations.Catalog;

// Dental service category configuration
public sealed class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategoryEntity>
{
    public void Configure(EntityTypeBuilder<ServiceCategoryEntity> builder)
    {
        builder
            .ToTable("ServiceCategories");

        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(ServiceCategoryEntity.Constraints.NameMaxLength);

        // Unique only among non deleted rows, deleted names can be reused
        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
