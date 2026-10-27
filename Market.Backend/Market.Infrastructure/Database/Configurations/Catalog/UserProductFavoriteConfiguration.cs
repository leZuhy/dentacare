namespace Market.Infrastructure.Database.Configurations.Catalog;

public class UserProductFavoriteConfiguration : IEntityTypeConfiguration<UserProductFavoriteEntity>
{
    public void Configure(EntityTypeBuilder<UserProductFavoriteEntity> builder)
    {
        builder
            .ToTable("UserProductFavorites");

        // Kompozitni UNIQUE index - sprečava duplikate (UserId, ProductId).
        // Ovo je alternativa za kompozitni primarni ključ koja radi sa BaseEntity (Id kao PK).
        // Filter [IsDeleted] = 0: soft-obrisani favorit ostaje u tabeli, pa bi bez filtera
        // ponovno dodavanje istog proizvoda u favorite palo na unique indexu.
        builder.HasIndex(x => new { x.UserId, x.ProductId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany(x => x.FavoriteProducts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Važi samo za hard delete (DELETE u bazi); soft delete korisnika ne briše favorite

        builder.HasOne(x => x.Product)
            .WithMany(x => x.FavoritedByUsers)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // Važi samo za hard delete (DELETE u bazi); soft delete proizvoda ne briše favorite
    }
}