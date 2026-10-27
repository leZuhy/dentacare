using Market.Application.Common;
using Market.Application.Modules.Catalog.ProductCategories.Queries.List;
using Market.Application.Modules.Sales.Orders.Queries.GetById;
using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.OrderTests.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class OrderIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public OrderIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates a product as admin (in the first enabled seeded category) and returns its id.
    /// </summary>
    private static async Task<int> CreateProductAsync(HttpClient adminClient, decimal price, bool isEnabled = true)
    {
        var categories = await adminClient.GetFromJsonAsync<PageResult<ListProductCategoriesQueryDto>>("/ProductCategories?onlyEnabled=true");

        var response = await adminClient.PostAsJsonAsync("/Products", new
        {
            Name = $"Order test {Guid.NewGuid():N}",
            Price = price,
            StockQuantity = 10,
            CategoryId = categories!.Items[0].Id,
            IsEnabled = isEnabled
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        return result!["id"];
    }

    /// <summary>
    /// Creates a draft order with one item (2 × <paramref name="productId"/>) and returns the order and its item id.
    /// </summary>
    private static async Task<(int OrderId, int ItemId)> CreateOrderAsync(HttpClient client, int productId)
    {
        var response = await client.PostAsJsonAsync("/Orders", new
        {
            Items = new[] { new { ProductId = productId, Quantity = 2m } }
        });
        response.EnsureSuccessStatusCode();

        var orderId = (await response.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
        var order = await client.GetFromJsonAsync<GetOrderByIdQueryDto>($"/Orders/{orderId}");
        return (orderId, Assert.Single(order!.Items).Id);
    }

    [Fact]
    public async Task Put_Order_ExistingItemGetsAnotherProduct_UsesPriceOfTheNewProduct()
    {
        // Arrange
        var admin = await _factory.GetAuthenticatedClientAsync();
        var firstProductId = await CreateProductAsync(admin, price: 10m);
        var secondProductId = await CreateProductAsync(admin, price: 25m);
        var (orderId, itemId) = await CreateOrderAsync(admin, firstProductId);

        // Act: the same item (Id), another product
        var response = await admin.PutAsJsonAsync($"/Orders/{orderId}", new
        {
            Items = new[] { new { Id = itemId, ProductId = secondProductId, Quantity = 2m } }
        });
        var order = await admin.GetFromJsonAsync<GetOrderByIdQueryDto>($"/Orders/{orderId}");

        // Assert: 2 × 25 = 50, minus the 5% demo discount
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var item = Assert.Single(order!.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal(secondProductId, item.Product.ProductId);
        Assert.Equal(25m, item.UnitPrice);
        Assert.Equal(50m, item.Subtotal);
        Assert.Equal(47.50m, item.Total);
        Assert.Equal(47.50m, order.TotalAmount);

        // Ticket 126: address and city of the user who placed the order (seeded admin)
        Assert.Equal("Maršala Tita 1", order.User.UserAddress);
        Assert.Equal("Sarajevo", order.User.UserCity);
    }

    [Fact]
    public async Task Put_Order_ExistingItemGetsDisabledProduct_ShouldReturnConflict()
    {
        // Arrange
        var admin = await _factory.GetAuthenticatedClientAsync();
        var enabledProductId = await CreateProductAsync(admin, price: 10m);
        var disabledProductId = await CreateProductAsync(admin, price: 10m, isEnabled: false);
        var (orderId, itemId) = await CreateOrderAsync(admin, enabledProductId);

        // Act
        var response = await admin.PutAsJsonAsync($"/Orders/{orderId}", new
        {
            Items = new[] { new { Id = itemId, ProductId = disabledProductId, Quantity = 1m } }
        });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
