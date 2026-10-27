using Market.Application.Common.Exceptions;
using Market.Application.Modules.Finance.ExchangeRates.Commands.Refresh;
using Market.Infrastructure.Common;
using Market.Shared.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using System.Net;
using System.Text;

namespace Market.Tests.CurrencyTests.UnitTests;

/// <summary>
/// Refresh of exchange rates through the Frankfurter provider, with a fake HTTP response (no network).
/// </summary>
public class ExchangeRateRefreshUnitTests
{
    // Shape of https://api.frankfurter.app/latest?from=EUR (shortened). The ECB does not publish RSD.
    private const string FrankfurterResponse =
        """{ "amount": 1.0, "base": "EUR", "date": "2026-10-16", "rates": { "CHF": 0.9350, "GBP": 0.8700, "USD": 1.1700 } }""";

    private static readonly DateTime EcbDateUtc = new(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc);

    private DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets a new database
            .Options;

        var context = new DatabaseContext(options, new FakeTimeProvider());
        context.Database.EnsureCreated(); // currencies from StaticDataSeeder
        return context;
    }

    private static FrankfurterExchangeRateProvider CreateProvider(string provider)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(FrankfurterResponse))
        {
            BaseAddress = new Uri("https://frankfurter.test/")
        };
        var options = Microsoft.Extensions.Options.Options.Create(new ExchangeRatesOptions { Provider = provider });

        return new FrankfurterExchangeRateProvider(httpClient, options, NullLogger<FrankfurterExchangeRateProvider>.Instance);
    }

    [Fact]
    public async Task Handle_StoresRatesToBam_AndReportsCurrenciesTheEcbDoesNotPublish()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var handler = new RefreshExchangeRatesCommandHandler(context, CreateProvider(ExchangeRatesOptions.ProviderFrankfurter));

        // Act
        var result = await handler.Handle(new RefreshExchangeRatesCommand(), CancellationToken.None);

        // Assert: CHF, EUR, GBP, USD stored; RSD skipped
        Assert.Equal(4, result.Updated);
        Assert.Equal(["RSD"], result.Skipped);
        Assert.Equal("Frankfurter (ECB)", result.Source);

        var usd = await context.ExchangeRates.SingleAsync(r => r.Currency!.Code == "USD");
        Assert.Equal(1.671650m, usd.RateToBase); // 1.95583 / 1.17
        Assert.Equal(EcbDateUtc, usd.ValidFromUtc);

        var eur = await context.ExchangeRates.SingleAsync(r => r.Currency!.Code == "EUR" && r.ValidFromUtc == EcbDateUtc);
        Assert.Equal(1.95583m, eur.RateToBase);
    }

    [Fact]
    public async Task Handle_SecondRefreshForTheSameDate_DoesNotDuplicateRates()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var handler = new RefreshExchangeRatesCommandHandler(context, CreateProvider(ExchangeRatesOptions.ProviderFrankfurter));
        await handler.Handle(new RefreshExchangeRatesCommand(), CancellationToken.None);

        // Act
        var second = await handler.Handle(new RefreshExchangeRatesCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(0, second.Updated);
        Assert.Equal(1, await context.ExchangeRates.CountAsync(r => r.Currency!.Code == "USD"));
    }

    [Fact]
    public async Task Handle_ProviderNone_ThrowsConflict()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var handler = new RefreshExchangeRatesCommandHandler(context, CreateProvider(ExchangeRatesOptions.ProviderNone));

        // Act + Assert
        await Assert.ThrowsAsync<MarketConflictException>(() =>
            handler.Handle(new RefreshExchangeRatesCommand(), CancellationToken.None));
    }

    private sealed class FakeHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
