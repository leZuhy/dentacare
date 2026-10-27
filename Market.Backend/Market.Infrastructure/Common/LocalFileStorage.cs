using Market.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace Market.Infrastructure.Common;

/// <summary>
/// <see cref="IFileStorage"/> that writes files into <c>wwwroot/uploads</c> of the API.
/// Program.cs serves wwwroot as static files, so a file saved as <c>wwwroot/uploads/products/x.webp</c>
/// is available anonymously at <c>https://localhost:7100/uploads/products/x.webp</c>.
/// <para>
/// Uploaded files are user data, not source code: <c>wwwroot/uploads/</c> is in .gitignore.
/// On a server with several instances, use shared storage (e.g. Azure Blob Storage) instead.
/// </para>
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    /// <summary>
    /// URL prefix and folder name of the uploads root inside wwwroot.
    /// </summary>
    private const string UploadsFolderName = "uploads";

    private readonly string _uploadsRootPath;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        // WebRootPath is null when the wwwroot folder did not exist at startup (Program.cs normally creates it).
        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _uploadsRootPath = Path.GetFullPath(Path.Combine(webRootPath, UploadsFolderName));
    }

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(folder) || !folder.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            throw new ArgumentException("Folder may contain only letters, digits and '-'.", nameof(folder));

        if (extension.Length < 2 || extension[0] != '.' || !extension[1..].All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("Extension must look like \".webp\".", nameof(extension));

        var folderPath = Path.Combine(_uploadsRootPath, folder);
        Directory.CreateDirectory(folderPath);

        // A random name: the client's file name is never used (it could contain "..\" or overwrite another file).
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        await using (var file = new FileStream(Path.Combine(folderPath, fileName), FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(file, ct);
        }

        return $"/{UploadsFolderName}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(relativeUrl);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps "/uploads/products/x.webp" to the physical path and makes sure the result is inside the uploads root.
    /// </summary>
    private string ToFullPath(string relativeUrl)
    {
        var prefix = $"/{UploadsFolderName}/";

        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith(prefix, StringComparison.Ordinal))
            throw new ArgumentException($"Only files under \"{prefix}\" can be deleted.", nameof(relativeUrl));

        var relativePath = relativeUrl[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_uploadsRootPath, relativePath));

        // GetFullPath resolves "..", so a path that escapes the uploads root is detected here.
        if (!fullPath.StartsWith(_uploadsRootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("The path points outside the uploads folder.", nameof(relativeUrl));

        return fullPath;
    }
}
