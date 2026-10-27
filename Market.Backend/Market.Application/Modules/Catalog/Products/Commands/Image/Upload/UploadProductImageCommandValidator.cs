namespace Market.Application.Modules.Catalog.Products.Commands.Image.Upload;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    /// <summary>
    /// Name of the multipart form field; errors are reported on it so the client can show them next to the file input.
    /// </summary>
    private const string FileFieldName = "File";

    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Odaberite sliku.")
            .Must(content => content.Length <= UploadProductImageCommand.MaxFileSizeBytes)
                .WithMessage("Slika može imati najviše 2 MB.")
            .OverridePropertyName(FileFieldName);

        RuleFor(x => x.ContentType)
            .Must(IsSupportedContentType)
                .WithMessage("Dozvoljeni formati slike su JPEG, PNG i WEBP.")
            .OverridePropertyName(FileFieldName);

        // The declared type is chosen by the client; the first bytes of the file must confirm it.
        RuleFor(x => x)
            .Must(x => string.Equals(ImageFileSignature.DetectContentType(x.Content), x.ContentType, StringComparison.OrdinalIgnoreCase))
                .WithMessage("Sadržaj fajla nije ispravna JPEG, PNG ili WEBP slika.")
            .OverridePropertyName(FileFieldName)
            .When(x => x.Content.Length > 0 && IsSupportedContentType(x.ContentType));
    }

    private static bool IsSupportedContentType(string contentType)
    {
        return ImageFileSignature.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }
}
