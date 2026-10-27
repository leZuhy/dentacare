namespace Market.Infrastructure.Database.Configurations.Catelog;

public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder
            .ToTable("Products");

        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(ProductEntity.Constraints.NameMaxLength);

        builder
            .Property(x => x.Description)
            .HasMaxLength(ProductEntity.Constraints.DescriptionMaxLength);

        builder
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        builder
            .Property(x => x.StockQuantity)
            .IsRequired();

        builder
            .Property(x => x.ImageUrl)
            .HasMaxLength(ProductEntity.Constraints.ImageUrlMaxLength);

        // Product names are unique (the validators check it first and return a field error).
        // Unique only among non-deleted products: a soft-deleted product keeps its row,
        // and without the filter its name could never be used again.
        // The default SQL Server collation is case-insensitive, like the check in the validators.
        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            // Restrict — the database rejects a hard DELETE of a category that still has products.
            // Soft delete (IsDeleted = true) is an UPDATE and never reaches this FK rule,
            // so the same rule is also checked in DeleteProductCategoryCommandHandler.
            .OnDelete(DeleteBehavior.Restrict);
    }
}