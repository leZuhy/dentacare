using Market.Application.Modules.Catalog.Products.Queries.List;
using Market.Application.Modules.Catalog.Products.Queries.GetById;
using Market.Application.Modules.Catalog.Products.Queries.NameAvailability;
using Market.Application.Modules.Catalog.Products.Commands.Create;
using Market.Application.Modules.Catalog.Products.Commands.Delete;
using Market.Application.Modules.Catalog.Products.Commands.Image.Delete;
using Market.Application.Modules.Catalog.Products.Commands.Image.Upload;
using Market.Application.Modules.Catalog.Products.Commands.Update;
using Market.Shared.Constants;

namespace Market.API.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<ActionResult<int>> Create(CreateProductCommand command, CancellationToken ct)
    {
        int id = await sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task Update(int id, UpdateProductCommand command, CancellationToken ct)
    {
        // ID from the route takes precedence
        command.Id = id;
        await sender.Send(command, ct);
        // no return -> 204 No Content
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteProductCommand { Id = id }, ct);
        // no return -> 204 No Content
    }

    // Public catalog: anonymous visitors can browse products (public "search products" page).
    // Disabled products (and products in disabled categories) are visible only to the administrator.
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<GetProductByIdQueryDto> GetById(int id, CancellationToken ct)
    {
        var product = await sender.Send(new GetProductByIdQuery { Id = id }, ct);
        return product; // if NotFoundException -> 404 via middleware
    }

    // Public catalog: anonymous visitors can browse products (public "search products" page).
    // Disabled products (and products in disabled categories) are listed only for the administrator.
    [HttpGet]
    [AllowAnonymous]
    public async Task<PageResult<ListProductsQueryDto>> List([FromQuery] ListProductsQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }

    /// <summary>
    /// Is the product name still free? <c>GET /Products/name-available?name=...&amp;excludeId=5</c>
    /// (excludeId = the product being edited). Create/Update check the same rule again.
    /// </summary>
    [HttpGet("name-available")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<CheckProductNameAvailabilityQueryDto> NameAvailable([FromQuery] CheckProductNameAvailabilityQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }

    /// <summary>
    /// Uploads or replaces the product image: <c>multipart/form-data</c> with the field <c>file</c>
    /// (JPEG, PNG or WEBP, at most 2 MB). The previous image is deleted.
    /// </summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Policy = PolicyNames.Admin)]
    [RequestSizeLimit(UploadProductImageCommand.MaxRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadProductImageCommand.MaxRequestSizeBytes)]
    public async Task<UploadProductImageCommandDto> UploadImage(int id, IFormFile? file, CancellationToken ct)
    {
        // IFormFile stays in the API layer: the command gets plain bytes and the declared content type.
        // A missing file becomes empty content, so the validator returns the usual field error on "file".
        var command = new UploadProductImageCommand
        {
            Id = id,
            Content = file is null ? [] : await ReadAllBytesAsync(file, ct),
            ContentType = file?.ContentType ?? string.Empty
        };

        var result = await sender.Send(command, ct);
        return result;
    }

    [HttpDelete("{id:int}/image")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task DeleteImage(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteProductImageCommand { Id = id }, ct);
        // no return -> 204 No Content
    }

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}
