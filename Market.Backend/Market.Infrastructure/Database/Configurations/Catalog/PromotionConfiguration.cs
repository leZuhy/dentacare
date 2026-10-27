namespace Market.Infrastructure.Database.Configurations.Catalog;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<PromotionEntity>
{
    public void Configure(EntityTypeBuilder<PromotionEntity> builder)
    {
        builder
            .ToTable("Promotions");

        builder
            .Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(PromotionEntity.Constraints.TitleMaxLength);

        builder
            .Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(PromotionEntity.Constraints.ImageUrlMaxLength);

        builder
            .Property(x => x.TargetUrl)
            .HasMaxLength(PromotionEntity.Constraints.TargetUrlMaxLength);

        // The home page reads active promotions ordered by SortOrder.
        builder
            .HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
