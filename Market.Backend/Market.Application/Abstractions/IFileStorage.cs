namespace Market.Application.Abstractions;

/// <summary>
/// Stores uploaded files (e.g. product images) and returns the URL they are served from.
/// <para>
/// The starter implementation (LocalFileStorage) writes into <c>wwwroot/uploads</c> of the API, which is
/// served as static files. Another implementation (e.g. Azure Blob Storage) can replace it without changing handlers.
/// </para>
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves <paramref name="content"/> as a new file with a random name in <paramref name="folder"/>
    /// (relative to the uploads root, e.g. "products") and returns its relative URL,
    /// e.g. <c>/uploads/products/3f2a…9c.webp</c>.
    /// </summary>
    /// <param name="content">File content.</param>
    /// <param name="folder">Sub-folder of the uploads root (letters, digits and '-' only).</param>
    /// <param name="extension">File extension including the dot, e.g. ".webp".</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file previously returned by <see cref="SaveAsync"/>. A missing file is ignored.
    /// A URL that does not point inside the uploads root (absolute URL, "..", other folders) is rejected
    /// with <see cref="ArgumentException"/>, so a tampered value can never delete other files.
    /// </summary>
    Task DeleteAsync(string relativeUrl, CancellationToken ct = default);
}
