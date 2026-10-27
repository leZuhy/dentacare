using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Sales;
using Market.Domain.Entities.Identity;

namespace Market.Infrastructure.Database.Seeders;

/// <summary>
/// Dynamic seeder koji se pokreće u runtime-u,
/// obično pri startu aplikacije (npr. u Program.cs).
/// Koristi se za unos demo/test podataka koji nisu dio migracije.
/// </summary>
public static class DynamicDataSeeder
{
    /// <summary>
    /// Demo kupci kojima pripadaju seed narudžbe.
    /// </summary>
    private static readonly string[] CustomerEmails =
    [
        "nina.bijedic@email.com",
        "iris.memic@email.com",
        "azra.smajic@email.com"
    ];

    public static async Task SeedAsync(DatabaseContext context)
    {
        // Šema baze se ne kreira ovdje: DatabaseInitializer prije seed-a pokreće migracije
        // (SQL Server) ili EnsureCreated (InMemory baza u testovima).

        await SeedProductCategoriesAsync(context);
        await SeedUsersAsync(context);
        await SeedProductsAsync(context);
        await SeedOrdersAsync(context);
        await SeedPromotionsAsync(context);
        await SeedExchangeRatesAsync(context);
    }

    private static async Task SeedProductCategoriesAsync(DatabaseContext context)
    {
        if (!await context.ProductCategories.AnyAsync())
        {
            context.ProductCategories.AddRange(
                new ProductCategoryEntity
                {
                    Name = "Računari",
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ProductCategoryEntity
                {
                    Name = "Mobilni uređaji",
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ProductCategoryEntity
                {
                    Name = "Periferija",
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ProductCategoryEntity
                {
                    Name = "Komponente",
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ProductCategoryEntity
                {
                    Name = "Audio oprema",
                    IsEnabled = false,
                    CreatedAtUtc = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Dynamic seed: product categories added.");
        }
    }

    /// <summary>
    /// Kreira demo korisnike ako ih još nema u bazi.
    /// Uloge i IsEnabled se postavljaju eksplicitno (baza nema default vrijednosti za njih).
    /// </summary>
    private static async Task SeedUsersAsync(DatabaseContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var hasher = new PasswordHasher<MarketUserEntity>();

        var admin = new MarketUserEntity
        {
            Firstname = "Admin",
            Lastname = "Market",
            Email = "admin@market.local",
            PasswordHash = hasher.HashPassword(null!, "Admin123!"),
            Address = "Maršala Tita 1",
            City = "Sarajevo",
            IsAdmin = true,
            IsManager = false,
            IsEmployee = false,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var manager = new MarketUserEntity
        {
            Firstname = "Manager",
            Lastname = "Market",
            Email = "manager@market.local",
            PasswordHash = hasher.HashPassword(null!, "Manager123!"),
            Address = "Ferhadija 12",
            City = "Sarajevo",
            IsAdmin = false,
            IsManager = true,
            IsEmployee = false,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var employee = new MarketUserEntity
        {
            Firstname = "Employee",
            Lastname = "Market",
            Email = "employee@market.local",
            PasswordHash = hasher.HashPassword(null!, "Employee123!"),
            Address = "Kralja Tvrtka 5",
            City = "Zenica",
            IsAdmin = false,
            IsManager = false,
            IsEmployee = true,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dummyForSwagger = new MarketUserEntity
        {
            Firstname = "Swagger",
            Lastname = "Demo",
            Email = "string",
            PasswordHash = hasher.HashPassword(null!, "string"),
            Address = "Zmaja od Bosne 8",
            City = "Sarajevo",
            IsAdmin = false,
            IsManager = false,
            IsEmployee = true,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dummyForTests = new MarketUserEntity
        {
            Firstname = "Test",
            Lastname = "User",
            Email = "test",
            PasswordHash = hasher.HashPassword(null!, "test123"),
            Address = "Titova 3",
            City = "Tuzla",
            IsAdmin = false,
            IsManager = false,
            IsEmployee = true,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Demo customers for orders
        var customer1 = new MarketUserEntity
        {
            Firstname = "Nina",
            Lastname = "Bijedić",
            Email = "nina.bijedic@email.com",
            PasswordHash = hasher.HashPassword(null!, "Customer123!"),
            Address = "Bulevar Meše Selimovića 15",
            City = "Sarajevo",
            IsAdmin = false,
            IsManager = false,
            IsEmployee = false,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
        };

        var customer2 = new MarketUserEntity
        {
            Firstname = "Iris",
            Lastname = "Memić",
            Email = "iris.memic@email.com",
            PasswordHash = hasher.HashPassword(null!, "Customer123!"),
            Address = "Kneza Domagoja 22",
            City = "Mostar",
            IsAdmin = false,
            IsManager = false,
            IsEmployee = false,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-25)
        };

        var customer3 = new MarketUserEntity
        {
            Firstname = "Azra",
            Lastname = "Smajić",
            Email = "azra.smajic@email.com",
            PasswordHash = hasher.HashPassword(null!, "Customer123!"),
            Address = "Trg slobode 4",
            City = "Tuzla",
            IsAdmin = false,
            IsManager = false,
            IsEmployee = false,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-20)
        };

        context.Users.AddRange(
            admin,
            manager,
            employee,
            dummyForSwagger,
            dummyForTests,
            customer1,
            customer2,
            customer3
        );

        await context.SaveChangesAsync();
        Console.WriteLine("✅ Dynamic seed: demo users added.");
    }

    /// <summary>
    /// Kreira demo proizvode ako ih još nema u bazi.
    /// </summary>
    private static async Task SeedProductsAsync(DatabaseContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        // Učitaj kategorije
        var categories = await context.ProductCategories
            .Where(c => c.IsEnabled)
            .ToListAsync();

        if (!categories.Any())
        {
            Console.WriteLine("⚠️  No categories found. Skipping product seed.");
            return;
        }

        var racunariCat = categories.FirstOrDefault(c => c.Name.Contains("Računari"));
        var mobilniCat = categories.FirstOrDefault(c => c.Name.Contains("Mobilni"));
        var periferijaCat = categories.FirstOrDefault(c => c.Name.Contains("Periferija"));
        var komponenteCat = categories.FirstOrDefault(c => c.Name.Contains("Komponente"));

        var products = new List<ProductEntity>();

        // Računari
        if (racunariCat != null)
        {
            products.AddRange(new[]
            {
                new ProductEntity
                {
                    Name = "Lenovo ThinkPad X1 Carbon Gen 11",
                    Description = "14\" FHD+ IPS, Intel i7-1365U, 16GB RAM, 512GB SSD, Windows 11 Pro",
                    Price = 2899.00m,
                    StockQuantity = 15,
                    CategoryId = racunariCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-20)
                },
                new ProductEntity
                {
                    Name = "Dell XPS 15 9530",
                    Description = "15.6\" 4K OLED Touch, Intel i9-13900H, 32GB RAM, 1TB SSD, RTX 4070",
                    Price = 3499.00m,
                    StockQuantity = 8,
                    CategoryId = racunariCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-18)
                },
                new ProductEntity
                {
                    Name = "HP Pavilion Gaming Desktop",
                    Description = "AMD Ryzen 7 5700G, 16GB RAM, 512GB SSD, RTX 3060 Ti, Windows 11",
                    Price = 1899.00m,
                    StockQuantity = 12,
                    CategoryId = racunariCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-15)
                },
                new ProductEntity
                {
                    Name = "ASUS ROG Zephyrus G14",
                    Description = "14\" QHD 165Hz, AMD Ryzen 9 7940HS, 32GB RAM, 1TB SSD, RTX 4060",
                    Price = 2699.00m,
                    StockQuantity = 6,
                    CategoryId = racunariCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
                }
            });
        }

        // Mobilni uređaji
        if (mobilniCat != null)
        {
            products.AddRange(new[]
            {
                new ProductEntity
                {
                    Name = "Samsung Galaxy S24 Ultra",
                    Description = "6.8\" AMOLED 2X, 12GB RAM, 512GB, Snapdragon 8 Gen 3, S Pen",
                    Price = 1699.00m,
                    StockQuantity = 25,
                    CategoryId = mobilniCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                },
                new ProductEntity
                {
                    Name = "Apple iPhone 15 Pro Max",
                    Description = "6.7\" Super Retina XDR, A17 Pro, 256GB, Titanium Design",
                    Price = 1899.00m,
                    StockQuantity = 18,
                    CategoryId = mobilniCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-9)
                },
                new ProductEntity
                {
                    Name = "Google Pixel 8 Pro",
                    Description = "6.7\" LTPO OLED, Google Tensor G3, 12GB RAM, 256GB",
                    Price = 1299.00m,
                    StockQuantity = 20,
                    CategoryId = mobilniCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-8)
                },
                new ProductEntity
                {
                    Name = "Xiaomi 14 Pro",
                    Description = "6.73\" AMOLED, Snapdragon 8 Gen 3, 12GB RAM, 512GB, Leica Camera",
                    Price = 1099.00m,
                    StockQuantity = 30,
                    CategoryId = mobilniCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-7)
                }
            });
        }

        // Periferija
        if (periferijaCat != null)
        {
            products.AddRange(new[]
            {
                new ProductEntity
                {
                    Name = "Logitech MX Master 3S",
                    Description = "Wireless Performance Mouse, 8K DPI, USB-C, Bluetooth",
                    Price = 149.00m,
                    StockQuantity = 50,
                    CategoryId = periferijaCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-6)
                },
                new ProductEntity
                {
                    Name = "Keychron K8 Pro Mechanical Keyboard",
                    Description = "Wireless TKL, Hot-swappable, RGB Backlight, Gateron Switch",
                    Price = 189.00m,
                    StockQuantity = 35,
                    CategoryId = periferijaCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
                },
                new ProductEntity
                {
                    Name = "Dell UltraSharp U2723DE",
                    Description = "27\" QHD IPS Monitor, USB-C Hub, 90W Power Delivery",
                    Price = 749.00m,
                    StockQuantity = 22,
                    CategoryId = periferijaCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-4)
                },
                new ProductEntity
                {
                    Name = "Blue Yeti USB Microphone",
                    Description = "Professional USB Condenser Microphone, Multiple Patterns",
                    Price = 169.00m,
                    StockQuantity = 40,
                    CategoryId = periferijaCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
                }
            });
        }

        // Komponente
        if (komponenteCat != null)
        {
            products.AddRange(new[]
            {
                new ProductEntity
                {
                    Name = "AMD Ryzen 9 7950X",
                    Description = "16-Core, 32-Thread Desktop Processor, 5.7 GHz Max Boost",
                    Price = 899.00m,
                    StockQuantity = 15,
                    CategoryId = komponenteCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new ProductEntity
                {
                    Name = "NVIDIA GeForce RTX 4080",
                    Description = "16GB GDDR6X, Ray Tracing, DLSS 3, Ada Lovelace Architecture",
                    Price = 1899.00m,
                    StockQuantity = 10,
                    CategoryId = komponenteCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new ProductEntity
                {
                    Name = "Corsair Vengeance DDR5 32GB",
                    Description = "2x16GB DDR5-6000 CL30, Intel XMP 3.0, AMD EXPO",
                    Price = 249.00m,
                    StockQuantity = 45,
                    CategoryId = komponenteCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Samsung 990 PRO NVMe SSD 2TB",
                    Description = "PCIe 4.0, 7450/6900 MB/s Read/Write, V-NAND Technology",
                    Price = 299.00m,
                    StockQuantity = 38,
                    CategoryId = komponenteCat.Id,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                }
            });
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Dynamic seed: {products.Count} products added.");
    }

    /// <summary>
    /// Kreira demo narudžbe (orders) sa order items.
    /// </summary>
    private static async Task SeedOrdersAsync(DatabaseContext context)
    {
        if (await context.Orders.AnyAsync())
            return;

        // Učitaj demo kupce (ne admin/manager/employee naloge) i proizvode
        var customers = await context.Users
            .Where(u => CustomerEmails.Contains(u.Email))
            .ToListAsync();

        // Narudžbe ispod biraju proizvode po poziciji (Skip/Take), pa redoslijed mora biti stabilan
        var products = await context.Products
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (customers.Count < CustomerEmails.Length || products.Count < 12)
        {
            Console.WriteLine("⚠️  Demo customers or products not found. Skipping orders seed.");
            return;
        }

        var orders = new List<OrderEntity>();
        var orderCounter = 1;

        // Order 1 - Completed (customer1, 15 dana prije)
        var customer1 = customers.Single(c => c.Email == CustomerEmails[0]);
        var order1Date = DateTime.UtcNow.AddDays(-15);
        var order1Products = products.Take(3).ToList();

        var order1Items = new List<OrderItemEntity>
        {
            CreateOrderItem(order1Products[0], 1, 0m), // Laptop
            CreateOrderItem(order1Products[1], 1, 10m), // Desktop sa 10% popustom
            CreateOrderItem(order1Products[2], 2, 0m)  // 2x monitor
        };

        var order1 = new OrderEntity
        {
            ReferenceNumber = $"ORD-{orderCounter++:D4}",
            MarketUserId = customer1.Id,
            OrderedAtUtc = order1Date,
            PaidAtUtc = order1Date.AddHours(2),
            Status = OrderStatusType.Completed,
            TotalAmount = order1Items.Sum(i => i.Total),
            Note = "Brza dostava molim.",
            Items = order1Items,
            CreatedAtUtc = order1Date
        };

        orders.Add(order1);

        // Order 2 - Paid (customer2, 10 dana prije)
        var customer2 = customers.Single(c => c.Email == CustomerEmails[1]);
        var order2Date = DateTime.UtcNow.AddDays(-10);
        var order2Products = products.Skip(4).Take(2).ToList(); // Phone + accessories

        var order2Items = new List<OrderItemEntity>
        {
            CreateOrderItem(order2Products[0], 1, 5m), // Phone sa 5% popustom
            CreateOrderItem(order2Products[1], 1, 0m)  // Accessories
        };

        var order2 = new OrderEntity
        {
            ReferenceNumber = $"ORD-{orderCounter++:D4}",
            MarketUserId = customer2.Id,
            OrderedAtUtc = order2Date,
            PaidAtUtc = order2Date.AddMinutes(30),
            Status = OrderStatusType.Paid,
            TotalAmount = order2Items.Sum(i => i.Total),
            Note = null,
            Items = order2Items,
            CreatedAtUtc = order2Date
        };

        orders.Add(order2);

        // Order 3 - Confirmed (customer3, 5 dana prije)
        var customer3 = customers.Single(c => c.Email == CustomerEmails[2]);
        var order3Date = DateTime.UtcNow.AddDays(-5);
        var order3Products = products.Skip(8).Take(4).ToList(); // PC components

        var order3Items = new List<OrderItemEntity>
        {
            CreateOrderItem(order3Products[0], 1, 0m), // CPU
            CreateOrderItem(order3Products[1], 1, 0m), // GPU
            CreateOrderItem(order3Products[2], 2, 15m), // 2x RAM sa 15% popustom
            CreateOrderItem(order3Products[3], 1, 0m)  // SSD
        };

        var order3 = new OrderEntity
        {
            ReferenceNumber = $"ORD-{orderCounter++:D4}",
            MarketUserId = customer3.Id,
            OrderedAtUtc = order3Date,
            PaidAtUtc = null,
            Status = OrderStatusType.Confirmed,
            TotalAmount = order3Items.Sum(i => i.Total),
            Note = "Kontaktirajte me prije dostave.",
            Items = order3Items,
            CreatedAtUtc = order3Date
        };

        orders.Add(order3);

        // Order 4 - Draft (customer1, 2 dana prije)
        var order4Date = DateTime.UtcNow.AddDays(-2);
        var order4Products = products.Skip(6).Take(2).ToList();

        var order4Items = new List<OrderItemEntity>
        {
            CreateOrderItem(order4Products[0], 1, 0m),
            CreateOrderItem(order4Products[1], 3, 20m) // 3x keyboards sa 20% popustom
        };

        var order4 = new OrderEntity
        {
            ReferenceNumber = $"ORD-{orderCounter++:D4}",
            MarketUserId = customer1.Id,
            OrderedAtUtc = order4Date,
            PaidAtUtc = null,
            Status = OrderStatusType.Draft,
            TotalAmount = order4Items.Sum(i => i.Total),
            Note = "Još razmišljam o boji...",
            Items = order4Items,
            CreatedAtUtc = order4Date
        };

        orders.Add(order4);

        // Order 5 - Cancelled (customer2, 7 dana prije)
        var order5Date = DateTime.UtcNow.AddDays(-7);
        var order5Products = products.Skip(3).Take(1).ToList();

        var order5Items = new List<OrderItemEntity>
        {
            CreateOrderItem(order5Products[0], 1, 0m)
        };

        var order5 = new OrderEntity
        {
            ReferenceNumber = $"ORD-{orderCounter++:D4}",
            MarketUserId = customer2.Id,
            OrderedAtUtc = order5Date,
            PaidAtUtc = null,
            Status = OrderStatusType.Cancelled,
            TotalAmount = order5Items.Sum(i => i.Total),
            Note = "Otkazan na zahtjev korisnika.",
            Items = order5Items,
            CreatedAtUtc = order5Date
        };

        orders.Add(order5);

        // Order 6 - Recent Paid (customer3, 1 dan prije)
        var order6Date = DateTime.UtcNow.AddDays(-1);
        var order6Products = products.Skip(10).Take(2).ToList();

        var order6Items = new List<OrderItemEntity>
        {
            CreateOrderItem(order6Products[0], 2, 0m),
            CreateOrderItem(order6Products[1], 1, 5m)
        };

        var order6 = new OrderEntity
        {
            ReferenceNumber = $"ORD-{orderCounter++:D4}",
            MarketUserId = customer3.Id,
            OrderedAtUtc = order6Date,
            PaidAtUtc = order6Date.AddHours(1),
            Status = OrderStatusType.Paid,
            TotalAmount = order6Items.Sum(i => i.Total),
            Note = "Express dostava.",
            Items = order6Items,
            CreatedAtUtc = order6Date
        };

        orders.Add(order6);

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Dynamic seed: {orders.Count} orders added with {orders.Sum(o => o.Items.Count)} total items.");
    }

    /// <summary>
    /// Kreira demo promocije (baneri na početnoj stranici) ako ih još nema u bazi.
    /// </summary>
    private static async Task SeedPromotionsAsync(DatabaseContext context)
    {
        if (await context.Promotions.AnyAsync())
            return;

        var promotions = new List<PromotionEntity>
        {
            new PromotionEntity
            {
                Title = "Zimska rasprodaja - Do 50% popusta!",
                ImageUrl = "https://images.unsplash.com/photo-1491002052546-bf38f186af56?w=800&h=300&fit=crop",
                TargetUrl = "/products?category=zimska-oprema",
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
            },
            new PromotionEntity
            {
                Title = "Novi proizvodi - Pogledajte kolekciju",
                ImageUrl = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=800&h=300&fit=crop",
                TargetUrl = "/products?sort=newest",
                SortOrder = 2,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-25)
            },
            new PromotionEntity
            {
                Title = "Besplatna dostava za narudžbe preko 50 BAM",
                ImageUrl = "https://images.unsplash.com/photo-1566576721346-d4a3b4eaeb55?w=800&h=300&fit=crop",
                TargetUrl = null,
                SortOrder = 3,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-20)
            },
            new PromotionEntity
            {
                Title = "Akcija - Brendirana oprema -30%",
                ImageUrl = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800&h=300&fit=crop",
                TargetUrl = "/products?brand=premium",
                SortOrder = 4,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-15)
            },
            new PromotionEntity
            {
                Title = "Loyalty program - Sakupljajte bodove",
                ImageUrl = "https://images.unsplash.com/photo-1556742393-d75f468bfcb0?w=800&h=300&fit=crop",
                TargetUrl = "/loyalty",
                SortOrder = 5,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            }
        };

        context.Promotions.AddRange(promotions);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Dynamic seed: {promotions.Count} promotions added.");
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

    /// <summary>
    /// Helper metoda za kreiranje order itema sa kalkulacijama.
    /// </summary>
    private static OrderItemEntity CreateOrderItem(
        ProductEntity product,
        decimal quantity,
        decimal discountPercent)
    {
        var unitPrice = product.Price;
        var subtotal = quantity * unitPrice;
        var discountAmount = discountPercent > 0
            ? Math.Round(subtotal * (discountPercent / 100), 2)
            : 0m;
        var total = subtotal - discountAmount;

        return new OrderItemEntity
        {
            ProductId = product.Id,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = subtotal,
            DiscountPercent = discountPercent > 0 ? discountPercent : null,
            DiscountAmount = discountAmount > 0 ? discountAmount : null,
            Total = total,
            Order = null, // EF sets the relationship when the item is added to OrderEntity.Items
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}