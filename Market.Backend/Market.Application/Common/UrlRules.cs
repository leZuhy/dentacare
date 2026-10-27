namespace Market.Application.Common;

/// <summary>
/// URL checks shared by validators.
/// </summary>
public static class UrlRules
{
    /// <summary>
    /// True for an absolute <c>http://</c> or <c>https://</c> URL (e.g. an image on another server).
    /// Other schemes such as <c>javascript:</c> or <c>data:</c> are rejected.
    /// </summary>
    public static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// True for an application route such as <c>/products?categoryId=2</c>.
    /// <c>//host/...</c> and backslashes are rejected, because browsers treat them as links to another site.
    /// </summary>
    public static bool IsRelativeRoute(string? value)
    {
        return value is not null
            && value.StartsWith('/')
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.Contains('\\')
            && !value.Any(char.IsWhiteSpace)
            && Uri.TryCreate(value, UriKind.Relative, out _);
    }
}
