using Market.Domain.Entities.Finance;

namespace Market.Infrastructure.Database.Configurations.Finance;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder
            .ToTable("Currencies");

        builder
            .Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(CurrencyEntity.Constraints.CodeLength)
            .IsFixedLength();

        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(CurrencyEntity.Constraints.NameMaxLength);

        builder
            .Property(x => x.Symbol)
            .IsRequired()
            .HasMaxLength(CurrencyEntity.Constraints.SymbolMaxLength);

        // Unique only among non-deleted currencies (soft delete keeps the row).
        builder
            .HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // At most one base currency: all rates are relative to it.
        builder
            .HasIndex(x => x.IsBase)
            .IsUnique()
            .HasFilter("[IsBase] = 1 AND [IsDeleted] = 0");
    }
}
