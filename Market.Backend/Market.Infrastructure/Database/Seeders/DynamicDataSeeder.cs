using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Identity;

namespace Market.Infrastructure.Database.Seeders;

/// <summary>
/// Dynamic seeder koji se pokreće u runtime-u,
/// obično pri startu aplikacije (npr. u Program.cs).
/// Koristi se za unos demo/test podataka koji nisu dio migracije.
/// </summary>
public static class DynamicDataSeeder
{
    public static async Task SeedAsync(DatabaseContext context)
    {
        // Šema baze se ne kreira ovdje: DatabaseInitializer prije seed-a pokreće migracije
        // (SQL Server) ili EnsureCreated (InMemory baza u testovima).

        await SeedUsersAsync(context);
        await SeedExchangeRatesAsync(context);
    }

    /// <summary>
    /// Kreira demo korisnike ako ih još nema u bazi.
    /// Role and IsEnabled are se explicitly (the db has no default values for them).
    /// </summary>
    private static async Task SeedUsersAsync(DatabaseContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var hasher = new PasswordHasher<MarketUserEntity>();

        var admin = new MarketUserEntity
        {
            Firstname ="Avdija",
            Lastname = "Halak",
            Email = "admin@dentacare.local",
            PasswordHash = hasher.HashPassword(null!, "Admin123!"),
            Address = "Marsala Tita 1",
            City = "Sarajevo",
            Role = Role.Admin,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dentist = new MarketUserEntity
        {
            Firstname = "Dino",
            Lastname = "Dentist",
            Email = "dentist@dentacare.local",
            PasswordHash = hasher.HashPassword(null!, "Dentist123!"),
            Address = "Ferhadija 12",
            City = "Sarajevo",
            Role = Role.Dentist,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var patient = new MarketUserEntity
        {
            Firstname = "Petar",
            Lastname = "Patient",
            Email = "patient@dentacare.local",
            PasswordHash = hasher.HashPassword(null!, "Patient123!"),
            Address = "Zenjak 5",
            City = "Travnik",
            Role = Role.Patient,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dummyForSwagger = new MarketUserEntity
        {
            Firstname = "Swagger",
            Lastname = "Demo",
            Email = "string",
            PasswordHash = hasher.HashPassword(null!, "string"),
            Role = Role.Patient,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dummyForTests = new MarketUserEntity
        {
            Firstname = "Test",
            Lastname = "User",
            Email = "test",
            PasswordHash = hasher.HashPassword(null!, "test123"),
            Role = Role.Patient,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Users.AddRange(admin,dentist,patient, dummyForSwagger, dummyForTests);

        await context.Users.AddRangeAsync(admin, dentist, patient);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Dynamic seed: demo users added.");
    }

    /// <summary>
    /// Približni kursevi za USD, CHF, GBP i RSD na četiri datuma u 2026, da konverzija radi odmah nakon pokretanja.
    /// Valute i fiksni kurs EUR (1,95583) dolaze iz migracije (StaticDataSeeder).
    /// Nisu stvarni kursevi: u produkciji ih unosi administrator ili se preuzimaju preko ExchangeRates:Provider.
    /// </summary>
    private static async Task SeedExchangeRatesAsync(DatabaseContext context)
    {
        const string source = "Seed (približno)";

        if (await context.ExchangeRates.AnyAsync(r => r.Source == source))
            return;

        // KM za 1 jedinicu valute, redom za 01.01, 01.04, 01.07. i 01.10.2026.
        var approximateRates = new Dictionary<string, decimal[]>
        {
            ["USD"] = [1.6650m, 1.6800m, 1.6600m, 1.6750m],
            ["CHF"] = [2.0900m, 2.1000m, 2.0850m, 2.0950m],
            ["GBP"] = [2.2400m, 2.2550m, 2.2300m, 2.2450m],
            ["RSD"] = [0.016690m, 0.016685m, 0.016692m, 0.016688m]
        };

        DateTime[] validFromDates =
        [
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        ];

        var codes = approximateRates.Keys.ToList();
        var currencyIdsByCode = await context.Currencies
            .Where(c => codes.Contains(c.Code))
            .ToDictionaryAsync(c => c.Code, c => c.Id);

        var rates = new List<ExchangeRateEntity>();
        foreach (var (code, values) in approximateRates)
        {
            if (!currencyIdsByCode.TryGetValue(code, out var currencyId))
                continue;

            for (var i = 0; i < validFromDates.Length; i++)
            {
                rates.Add(new ExchangeRateEntity
                {
                    CurrencyId = currencyId,
                    RateToBase = values[i],
                    ValidFromUtc = validFromDates[i],
                    Source = source,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        context.ExchangeRates.AddRange(rates);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Dynamic seed: {rates.Count} approximate exchange rates added.");
    }
}