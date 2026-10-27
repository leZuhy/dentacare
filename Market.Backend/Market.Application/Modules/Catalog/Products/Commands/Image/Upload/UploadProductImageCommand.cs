namespace Market.Application.Modules.Catalog.Products.Commands.Image.Upload;

/// <summary>
/// Uploads (or replaces) the image of a product.
/// <para>
/// The file arrives in the controller as ASP.NET Core <c>IFormFile</c>, but the Application layer must not
/// depend on ASP.NET Core types. The controller therefore copies the file into this plain command
/// (bytes + declared content type). The copy is small: the request size is limited to
/// <see cref="MaxFileSizeBytes"/> plus a little multipart overhead (<see cref="MaxRequestSizeBytes"/>).
/// Because the whole content is in memory, the validator can check the size, the type and the file signature,
/// and the handler can be unit tested without HTTP.
/// </para>
/// </summary>
public sealed class UploadProductImageCommand : IRequest<UploadProductImageCommandDto>
{
    /// <summary>
    /// Largest accepted image: 2 MB.
    /// </summary>
    public const int MaxFileSizeBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Request size limit for the upload endpoint: the image plus multipart headers and boundaries.
    /// </summary>
    public const int MaxRequestSizeBytes = MaxFileSizeBytes + 64 * 1024;

    /// <summary>
    /// Product identifier (from the route).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// File content; empty when the client did not send a file.
    /// </summary>
    public byte[] Content { get; init; } = [];

    /// <summary>
    /// Content type declared by the client (e.g. "image/png"); it must match the file signature.
    /// </summary>
    public string ContentType { get; init; } = string.Empty;
}
