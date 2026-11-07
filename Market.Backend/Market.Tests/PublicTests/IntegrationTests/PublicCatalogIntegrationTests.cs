using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Market.Domain.Entities.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Market.Tests.PublicTests.IntegrationTests;

/// <summary>
/// Public catalog (api/public): no token needed, only active services and enabled dentists, no dentist email.
/// Every test adds its own data with a random token in the names and filters by it,
/// so seeded data and other tests don't change the result.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PublicCatalogIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public PublicCatalogIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // only the fields the tests check
    private sealed record ServiceItem(int Id, string Name, decimal PriceBam);
    private sealed record DentistItem(int Id, string Firstname, string Lastname);
    private sealed record PageResponse<T>(List<T> Items, int TotalItems);
    private sealed record LookupItem(int Id, string Name);

    private static string NewToken() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// A new category with two active services (30 KM and 200 KM) and one inactive (50 KM).
    /// </summary>
    private async Task<(int CategoryId, string Token)> AddServicesAsync()
    {
        var token = NewToken();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var category = new ServiceCategoryEntity { Name = $"Kategorija {token}" };
        db.DentalServices.AddRange(
            new DentalServiceEntity { Category = category, Name = $"Jeftina {token}", PriceBam = 30m, DurationMinutes = 30 },
            new DentalServiceEntity { Category = category, Name = $"Skupa {token}", PriceBam = 200m, DurationMinutes = 60 },
            new DentalServiceEntity { Category = category, Name = $"Neaktivna {token}", PriceBam = 50m, DurationMinutes = 30, IsActive = false });
        await db.SaveChangesAsync();

        return (category.Id, token);
    }

    /// <summary>
    /// A new specialization with a disabled dentist and (optionally) an enabled one.
    /// Both have the token in the last name, so a search by token finds only them.
    /// </summary>
    private async Task<(int SpecializationId, string Token)> AddDentistsAsync(bool addEnabled = true)
    {
        var token = NewToken();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var specialization = new SpecializationEntity { Name = $"Specijalizacija {token}" };
        if (addEnabled)
            db.Dentists.Add(NewDentist(specialization, "Aktivni", token, isEnabled: true));
        db.Dentists.Add(NewDentist(specialization, "Ugaseni", token, isEnabled: false));
        await db.SaveChangesAsync();

        return (specialization.Id, token);
    }

    private static DentistEntity NewDentist(SpecializationEntity specialization, string firstname, string token, bool isEnabled) => new()
    {
        Specialization = specialization,
        Bio = "Test biografija",
        User = new MarketUserEntity
        {
            Firstname = firstname,
            Lastname = $"Dentist{token}",
            Email = $"{firstname.ToLower()}-{token}@test.local",
            PasswordHash = "x",
            Role = Role.Dentist,
            IsEnabled = isEnabled
        }
    };

    // === services ===

    [Fact]
    public async Task ListServices_WithoutToken_ReturnsOnlyActiveServices()
    {
        // Arrange
        var (categoryId, _) = await AddServicesAsync();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"api/public/services?categoryId={categoryId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PageResponse<ServiceItem>>();
        Assert.Equal(2, page!.TotalItems);
        Assert.DoesNotContain(page.Items, s => s.Name.StartsWith("Neaktivna"));
    }

    [Theory]
    [InlineData("priceAsc", "Jeftina")]
    [InlineData("priceDesc", "Skupa")]
    public async Task ListServices_SortByPrice_ReturnsInOrder(string sort, string expectedFirst)
    {
        // Arrange
        var (categoryId, _) = await AddServicesAsync();
        var client = _factory.CreateClient();

        // Act
        var page = await client.GetFromJsonAsync<PageResponse<ServiceItem>>(
            $"api/public/services?categoryId={categoryId}&sort={sort}");

        // Assert
        Assert.StartsWith(expectedFirst, page!.Items[0].Name);
    }

    [Fact]
    public async Task ListServices_MaxPrice_HidesMoreExpensive()
    {
        // Arrange
        var (categoryId, _) = await AddServicesAsync();
        var client = _factory.CreateClient();

        // Act
        var page = await client.GetFromJsonAsync<PageResponse<ServiceItem>>(
            $"api/public/services?categoryId={categoryId}&maxPriceBam=100");

        // Assert: 30 KM stays, 200 KM is hidden
        var service = Assert.Single(page!.Items);
        Assert.StartsWith("Jeftina", service.Name);
    }

    [Fact]
    public async Task ListServices_Search_IgnoresCase()
    {
        // Arrange
        var (_, token) = await AddServicesAsync();
        var client = _factory.CreateClient();
        var search = Uri.EscapeDataString($"JEFTINA {token.ToUpper()}");

        // Act
        var page = await client.GetFromJsonAsync<PageResponse<ServiceItem>>($"api/public/services?search={search}");

        // Assert
        var service = Assert.Single(page!.Items);
        Assert.StartsWith("Jeftina", service.Name);
    }

    [Theory]
    [InlineData("sort=7")]
    [InlineData("maxPriceBam=0")]
    public async Task ListServices_InvalidFilter_Returns400(string queryString)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"api/public/services?{queryString}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListServiceCategories_HidesCategoryWithoutActiveServices()
    {
        // Arrange: one category with active services, one with only an inactive service
        var (activeCategoryId, _) = await AddServicesAsync();

        int inactiveCategoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var category = new ServiceCategoryEntity { Name = $"Prazna {NewToken()}" };
            db.DentalServices.Add(new DentalServiceEntity
            {
                Category = category,
                Name = $"Ugasena {NewToken()}",
                PriceBam = 40m,
                DurationMinutes = 30,
                IsActive = false
            });
            await db.SaveChangesAsync();
            inactiveCategoryId = category.Id;
        }

        var client = _factory.CreateClient();

        // Act
        var categories = await client.GetFromJsonAsync<List<LookupItem>>("api/public/service-categories");

        // Assert
        Assert.Contains(categories!, c => c.Id == activeCategoryId);
        Assert.DoesNotContain(categories!, c => c.Id == inactiveCategoryId);
    }

    // === dentists ===

    [Fact]
    public async Task ListDentists_WithoutToken_ReturnsEnabledDentistsWithoutEmail()
    {
        // Arrange
        var (_, token) = await AddDentistsAsync();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"api/public/dentists?search={token}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"email\"", json); // the field must not exist at all

        var page = JsonSerializer.Deserialize<PageResponse<DentistItem>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var dentist = Assert.Single(page!.Items); // the disabled one is hidden
        Assert.Equal("Aktivni", dentist.Firstname);
    }

    [Fact]
    public async Task ListDentists_SearchByEmail_FindsNothing()
    {
        // Arrange
        var (_, token) = await AddDentistsAsync();
        var client = _factory.CreateClient();

        // Act: part of the enabled dentist's email
        var page = await client.GetFromJsonAsync<PageResponse<DentistItem>>(
            $"api/public/dentists?search={Uri.EscapeDataString($"aktivni-{token}@")}");

        // Assert
        Assert.Equal(0, page!.TotalItems);
    }

    [Fact]
    public async Task ListSpecializations_HidesSpecializationWithOnlyDisabledDentists()
    {
        // Arrange
        var (withEnabledId, _) = await AddDentistsAsync();
        var (onlyDisabledId, _) = await AddDentistsAsync(addEnabled: false);
        var client = _factory.CreateClient();

        // Act
        var specializations = await client.GetFromJsonAsync<List<LookupItem>>("api/public/specializations");

        // Assert
        Assert.Contains(specializations!, s => s.Id == withEnabledId);
        Assert.DoesNotContain(specializations!, s => s.Id == onlyDisabledId);
    }
}