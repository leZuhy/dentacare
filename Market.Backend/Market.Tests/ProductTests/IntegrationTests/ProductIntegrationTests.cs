using Market.Application.Common;
using Market.Application.Modules.Catalog.ProductCategories.Queries.List;
using Market.Application.Modules.Catalog.Products.Commands.Image.Upload;
using Market.Application.Modules.Catalog.Products.Queries.List;
using Market.Application.Modules.Catalog.Products.Queries.NameAvailability;
using Market.Shared.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Tests.ProductTests.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ProductIntegrationTests
{
    // Smallest valid PNG: 1×1 transparent pixel.
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private readonly CustomWebApplicationFactory<Program> _factory;

    public ProductIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates a product as admin (in the first seeded category) and returns its id.
    /// </summary>
    private static async Task<int> CreateProductAsync(HttpClient adminClient, string name, bool isEnabled)
    {
        var categories = await adminClient.GetFromJsonAsync<PageResult<ListProductCategoriesQueryDto>>("/ProductCategories?onlyEnabled=true");

        var response = await adminClient.PostAsJsonAsync("/Products", new
        {
            Name = name,
            Price = 10m,
            StockQuantity = 5,
            CategoryId = categories!.Items[0].Id,
            IsEnabled = isEnabled
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        return result!["id"];
    }

    [Fact]
    public async Task Get_Products_DisabledProduct_IsListedOnlyForAdmin()
    {
        // Arrange
        var admin = await _factory.GetAuthenticatedClientAsync();
        var anonymous = _factory.CreateClient();
        var name = $"Disabled {Guid.NewGuid():N}";
        var id = await CreateProductAsync(admin, name, isEnabled: false);

        // Act
        var adminList = await admin.GetFromJsonAsync<PageResult<ListProductsQueryDto>>($"/Products?search={name}");
        var anonymousList = await anonymous.GetFromJsonAsync<PageResult<ListProductsQueryDto>>($"/Products?search={name}");
        var anonymousGet = await anonymous.GetAsync($"/Products/{id}");

        // Assert
        Assert.Single(adminList!.Items);
        Assert.Empty(anonymousList!.Items);
        Assert.Equal(HttpStatusCode.NotFound, anonymousGet.StatusCode);
    }

    [Fact]
    public async Task Post_Product_DuplicateNameInDifferentCase_ShouldReturnFieldError()
    {
        // Arrange
        var admin = await _factory.GetAuthenticatedClientAsync();
        var name = $"Unique {Guid.NewGuid():N}";
        await CreateProductAsync(admin, name, isEnabled: true);

        // Act
        var available = await admin.GetFromJsonAsync<CheckProductNameAvailabilityQueryDto>(
            $"/Products/name-available?name={name.ToUpperInvariant()}");
        var response = await admin.PostAsJsonAsync("/Products", new { Name = name.ToUpperInvariant(), Price = 1m, CategoryId = 1 });

        // Assert
        Assert.False(available!.Available);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Contains(error!.Errors!, e => e.Field == "name");
    }

    [Fact]
    public async Task Post_ProductImage_IsServedAnonymously_AndDeleted()
    {
        // Arrange
        var admin = await _factory.GetAuthenticatedClientAsync();
        var anonymous = _factory.CreateClient();
        var id = await CreateProductAsync(admin, $"Image {Guid.NewGuid():N}", isEnabled: true);

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(OnePixelPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "pixel.png");

        // Act
        var uploadResponse = await admin.PostAsync($"/Products/{id}/image", form);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadProductImageCommandDto>();
        var servedImage = await anonymous.GetAsync(upload!.ImageUrl);
        var deleteResponse = await admin.DeleteAsync($"/Products/{id}/image");
        var afterDelete = await anonymous.GetAsync(upload.ImageUrl);

        // Assert
        Assert.StartsWith("/uploads/products/", upload.ImageUrl);
        Assert.EndsWith(".png", upload.ImageUrl);
        Assert.Equal(HttpStatusCode.OK, servedImage.StatusCode);
        Assert.Equal("image/png", servedImage.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Post_ProductImage_WrongSignature_ShouldReturnFieldErrorOnFile()
    {
        // Arrange: a PNG declared as JPEG
        var admin = await _factory.GetAuthenticatedClientAsync();
        var id = await CreateProductAsync(admin, $"Fake {Guid.NewGuid():N}", isEnabled: true);

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(OnePixelPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "pixel.jpg");

        // Act
        var response = await admin.PostAsync($"/Products/{id}/image", form);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("file", Assert.Single(error!.Errors!).Field);
    }
}
