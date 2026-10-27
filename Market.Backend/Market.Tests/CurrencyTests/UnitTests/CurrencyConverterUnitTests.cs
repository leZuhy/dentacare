using Market.Application.Common.Exceptions;
using Market.Domain.Entities.Finance;
using Market.Infrastructure.Common;
using Microsoft.Extensions.Time.Testing;

namespace Market.Tests.CurrencyTests.UnitTests;

public class CurrencyConverterUnitTests
{
    private const int UsdCurrencyId = 3; // see StaticDataSeeder

    private static readonly DateTime Jan1 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Jul1 = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// New InMemory database with the static seed (currencies BAM, EUR, USD, CHF, GBP, RSD and EUR = 1.95583 KM)
    /// and two USD rates: 1.80 KM from 1 Jan 2026 and 1.70 KM from 1 Jul 2026.
    /// </summary>
    private DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets a new database
            .Options;

        var context = new DatabaseContext(options, _clock);
        context.Database.EnsureCreated(); // applies HasData from StaticDataSeeder

        context.ExchangeRates.AddRange(
            new ExchangeRateEntity { CurrencyId = UsdCurrencyId, RateToBase = 1.80m, ValidFromUtc = Jan1, Source = "Test" },
            new ExchangeRateEntity { CurrencyId = UsdCurrencyId, RateToBase = 1.70m, ValidFromUtc = Jul1, Source = "Test" });
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task ConvertAsync_EurToBam_UsesFixedRate()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var converter = new CurrencyConverter(context, _clock);

        // Act (codes are case-insensitive)
        var result = await converter.ConvertAsync(100m, "eur", "BAM");

        // Assert: 100 × 1.95583 = 195.583 → 195.58
        Assert.Equal("EUR", result.From);
        Assert.Equal("BAM", result.To);
        Assert.Equal(195.58m, result.ConvertedAmount);
        Assert.Equal(1.95583m, result.Rate);
        Assert.Equal(1.95583m, result.FromRateToBase);
        Assert.Equal(1m, result.ToRateToBase);
    }

    [Fact]
    public async Task ConvertAsync_BamToEur_RoundsToTargetDecimalPlaces()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var converter = new CurrencyConverter(context, _clock);

        // Act
        var result = await converter.ConvertAsync(100m, "BAM", "EUR");

        // Assert: 100 / 1.95583 = 51.1291...; the rate is rounded to 6 decimals
        Assert.Equal(51.13m, result.ConvertedAmount);
        Assert.Equal(0.511292m, result.Rate);
    }

    [Fact]
    public async Task ConvertAsync_MidpointIsRoundedAwayFromZero()
    {
        // Arrange: 1 USD = 2 KM from 1 Oct 2026
        using var context = GetInMemoryDbContext();
        context.ExchangeRates.Add(new ExchangeRateEntity
        {
            CurrencyId = UsdCurrencyId,
            RateToBase = 2m,
            ValidFromUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Source = "Test"
        });
        await context.SaveChangesAsync();
        var converter = new CurrencyConverter(context, _clock);

        // Act: 0.25 KM = 0.125 USD
        var result = await converter.ConvertAsync(0.25m, "BAM", "USD");

        // Assert: AwayFromZero gives 0.13 (banker's rounding would give 0.12)
        Assert.Equal(0.13m, result.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertAsync_UsdToEur_ConvertsThroughBaseCurrency()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var converter = new CurrencyConverter(context, _clock);

        // Act (now = 19 Oct 2026, so the USD rate from 1 Jul applies)
        var result = await converter.ConvertAsync(100m, "USD", "EUR");

        // Assert: 100 × 1.70 / 1.95583 = 86.9196...
        Assert.Equal(86.92m, result.ConvertedAmount);
        Assert.Equal(0.869196m, result.Rate);
        Assert.Equal(1.70m, result.FromRateToBase);
        Assert.Equal(1.95583m, result.ToRateToBase);
        Assert.Equal(Jul1, result.RateDateUtc); // the later of the two rate dates
    }

    [Fact]
    public async Task ConvertAsync_HistoricalDate_UsesRateValidAtThatDate()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var converter = new CurrencyConverter(context, _clock);

        // Act: 15 Mar 2026 is before the 1 Jul rate, so the 1 Jan rate (1.80) applies
        var result = await converter.ConvertAsync(100m, "USD", "BAM", new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.Equal(180.00m, result.ConvertedAmount);
        Assert.Equal(Jan1, result.RateDateUtc);
    }

    [Fact]
    public async Task ConvertAsync_UnknownCurrency_ThrowsNotFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var converter = new CurrencyConverter(context, _clock);

        // Act + Assert
        await Assert.ThrowsAsync<MarketNotFoundException>(() => converter.ConvertAsync(100m, "XYZ", "BAM"));
    }

    [Fact]
    public async Task ConvertAsync_DisabledCurrency_ThrowsNotFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var usd = await context.Currencies.SingleAsync(c => c.Code == "USD");
        usd.IsEnabled = false;
        await context.SaveChangesAsync();
        var converter = new CurrencyConverter(context, _clock);

        // Act + Assert
        await Assert.ThrowsAsync<MarketNotFoundException>(() => converter.ConvertAsync(100m, "USD", "BAM"));
    }

    [Fact]
    public async Task ConvertAsync_NoRateValidYet_ThrowsBusinessRule()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var converter = new CurrencyConverter(context, _clock);

        // Act + Assert: the first USD rate is valid from 1 Jan 2026
        await Assert.ThrowsAsync<MarketBusinessRuleException>(() =>
            converter.ConvertAsync(100m, "USD", "BAM", new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc)));
    }
}
