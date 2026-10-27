using Market.Domain.Entities.Finance;

namespace Market.Infrastructure.Database.Seeders;

/// <summary>
/// Static (reference) data that every environment needs, including production.
/// It is written into the migration with <c>HasData</c>, so it needs fixed Ids and fixed dates
/// (a value such as <c>DateTime.UtcNow</c> would change the model and create a new migration every time).
/// Demo data for development belongs in <see cref="DynamicDataSeeder"/>.
/// </summary>
public static class StaticDataSeeder
{
    private static readonly DateTime SeedDateUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedCurrencies(modelBuilder);
    }

    /// <summary>
    /// Currencies and the fixed EUR rate. Rates of the other currencies change every day,
    /// so they are not reference data: in development they come from <see cref="DynamicDataSeeder"/>,
    /// otherwise the administrator enters them (or enables the exchange rate provider).
    /// </summary>
    private static void SeedCurrencies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrencyEntity>().HasData(
            new CurrencyEntity { Id = 1, Code = "BAM", Name = "Konvertibilna marka", Symbol = "KM", DecimalPlaces = 2, IsBase = true, IsEnabled = true, CreatedAtUtc = SeedDateUtc },
            new CurrencyEntity { Id = 2, Code = "EUR", Name = "Euro", Symbol = "€", DecimalPlaces = 2, IsBase = false, IsEnabled = true, CreatedAtUtc = SeedDateUtc },
            new CurrencyEntity { Id = 3, Code = "USD", Name = "Američki dolar", Symbol = "$", DecimalPlaces = 2, IsBase = false, IsEnabled = true, CreatedAtUtc = SeedDateUtc },
            new CurrencyEntity { Id = 4, Code = "CHF", Name = "Švicarski franak", Symbol = "CHF", DecimalPlaces = 2, IsBase = false, IsEnabled = true, CreatedAtUtc = SeedDateUtc },
            new CurrencyEntity { Id = 5, Code = "GBP", Name = "Britanska funta", Symbol = "£", DecimalPlaces = 2, IsBase = false, IsEnabled = true, CreatedAtUtc = SeedDateUtc },
            new CurrencyEntity { Id = 6, Code = "RSD", Name = "Srpski dinar", Symbol = "RSD", DecimalPlaces = 2, IsBase = false, IsEnabled = true, CreatedAtUtc = SeedDateUtc });

        // The KM has been pegged to the euro since 1999 (currency board): 1 EUR = 1.95583 KM.
        modelBuilder.Entity<ExchangeRateEntity>().HasData(
            new ExchangeRateEntity
            {
                Id = 1,
                CurrencyId = 2,
                RateToBase = ExchangeRateEntity.EurRateToBam,
                ValidFromUtc = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Source = "CBBiH (fiksni kurs)",
                CreatedAtUtc = SeedDateUtc
            });
    }
}
