namespace Market.Application.Common;

/// <summary>
/// Recognizes JPEG, PNG and WEBP images by their first bytes ("magic numbers").
/// The file name and the Content-Type header are chosen by the client and can be anything,
/// so an upload is accepted only when its content really starts like the declared image type.
/// </summary>
public static class ImageFileSignature
{
    public const string JpegContentType = "image/jpeg";
    public const string PngContentType = "image/png";
    public const string WebpContentType = "image/webp";

    /// <summary>
    /// Content types accepted for image uploads.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedContentTypes = [JpegContentType, PngContentType, WebpContentType];

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
    private static readonly byte[] WebpSignature = "WEBP"u8.ToArray();

    /// <summary>
    /// Returns the content type detected from the file header, or null when it is not a JPEG, PNG or WEBP image.
    /// </summary>
    public static string? DetectContentType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(JpegSignature))
            return JpegContentType;

        if (content.StartsWith(PngSignature))
            return PngContentType;

        // WEBP: "RIFF" + 4 bytes of file size + "WEBP"
        if (content.Length >= 12 && content.StartsWith(RiffSignature) && content.Slice(8, 4).SequenceEqual(WebpSignature))
            return WebpContentType;

        return null;
    }

    /// <summary>
    /// File extension (with the dot) used when an image of the given content type is stored.
    /// </summary>
    public static string GetFileExtension(string contentType) => contentType switch
    {
        JpegContentType => ".jpg",
        PngContentType => ".png",
        WebpContentType => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unsupported image content type.")
    };
}
