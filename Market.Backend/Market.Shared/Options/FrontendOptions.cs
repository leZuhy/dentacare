namespace Market.Shared.Options;

/// <summary>
/// "Frontend" section: where the Angular application runs, for links in emails
/// (e.g. <c>{BaseUrl}/auth/reset-password?email=...&amp;token=...</c>).
/// </summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// e.g. "http://localhost:4200" (appsettings.Development.json and appsettings.Testing.json).
    /// Required outside Development, Testing and IntegrationTests (checked on startup).
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;
}
