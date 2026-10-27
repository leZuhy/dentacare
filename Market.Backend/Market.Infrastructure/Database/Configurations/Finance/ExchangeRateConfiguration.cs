using Market.Domain.Entities.Finance;

namespace Market.Infrastructure.Database.Configurations.Finance;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRateEntity>
{
    public void Configure(EntityTypeBuilder<ExchangeRateEntity> builder)
    {
        builder
            .ToTable("ExchangeRates");

        // decimal(18,6) instead of the app-wide decimal(18,2): rates such as 0.016688 (RSD) need 6 decimals.
        builder
            .Property(x => x.RateToBase)
            .HasPrecision(ExchangeRateEntity.Constraints.RatePrecision, ExchangeRateEntity.Constraints.RateScale);

        builder
            .Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(ExchangeRateEntity.Constraints.SourceMaxLength);

        // One rate per currency and moment; the index also serves "latest rate with ValidFromUtc <= date".
        builder
            .HasIndex(x => new { x.CurrencyId, x.ValidFromUtc })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder
            .HasOne(x => x.Currency)
            .WithMany(x => x.ExchangeRates)
            .HasForeignKey(x => x.CurrencyId)
            // Restrict — the database rejects a hard DELETE of a currency that has rates.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
