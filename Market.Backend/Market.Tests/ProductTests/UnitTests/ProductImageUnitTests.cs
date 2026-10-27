using Market.Application.Modules.Catalog.Products.Commands.Image.Upload;
using Market.Infrastructure.Common;
using Microsoft.Extensions.FileProviders;

namespace Market.Tests.ProductTests.UnitTests;

public class ProductImageUnitTests
{
    // First bytes of real files ("magic numbers"); the rest of the content does not matter for the check.
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8];

    private readonly UploadProductImageCommandValidator _validator = new();

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    public void Validator_ContentMatchingDeclaredType_IsValid(string contentType)
    {
        // Arrange
        var content = contentType switch { "image/png" => Png, "image/jpeg" => Jpeg, _ => Webp };
        var command = new UploadProductImageCommand { Id = 1, Content = content, ContentType = contentType };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_ContentNotMatchingDeclaredType_IsInvalidOnFileField()
    {
        // Arrange: a text file renamed to .png
        var command = new UploadProductImageCommand { Id = 1, Content = "hello world"u8.ToArray(), ContentType = "image/png" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("File", error.PropertyName);
    }

    [Fact]
    public void Validator_FileLargerThan2Mb_IsInvalid()
    {
        // Arrange
        var content = new byte[UploadProductImageCommand.MaxFileSizeBytes + 1];
        Png.CopyTo(content, 0);
        var command = new UploadProductImageCommand { Id = 1, Content = content, ContentType = "image/png" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "File" && e.ErrorMessage.Contains("2 MB"));
    }

    [Fact]
    public async Task LocalFileStorage_SavesAndDeletesFileInsideUploadsFolder()
    {
        // Arrange
        var webRoot = Directory.CreateTempSubdirectory("market-tests-").FullName;
        var storage = new LocalFileStorage(new TestWebHostEnvironment(webRoot));

        try
        {
            // Act
            var url = await storage.SaveAsync(new MemoryStream(Png), "products", ".png");
            var path = Path.Combine(webRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var existedAfterSave = File.Exists(path);
            await storage.DeleteAsync(url);

            // Assert
            Assert.StartsWith("/uploads/products/", url);
            Assert.EndsWith(".png", url);
            Assert.True(existedAfterSave);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("/uploads/../appsettings.json")]
    [InlineData("/uploads/products/../../secret.txt")]
    [InlineData("https://example.com/uploads/x.png")]
    [InlineData("/other/x.png")]
    public async Task LocalFileStorage_DeleteOutsideUploadsFolder_IsRejected(string url)
    {
        // Arrange
        var storage = new LocalFileStorage(new TestWebHostEnvironment(Path.GetTempPath()));

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(url));
    }

    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Market.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "UnitTests";
    }
}
