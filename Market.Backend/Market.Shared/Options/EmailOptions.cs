using System.ComponentModel.DataAnnotations;

namespace Market.Shared.Options;

/// <summary>
/// "Email:Smtp" section: the SMTP server used to send emails (e.g. the "forgot password" link).
/// <para>
/// Empty <see cref="Host"/> (default): emails are not sent but written to the backend log (LoggingEmailSender).
/// That is allowed only in Development, Testing and IntegrationTests; every other environment stops on startup.
/// For a local SMTP test server such as Mailpit or Papercut: Host "localhost", Port 1025, EnableSsl false.
/// </para>
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email:Smtp";

    /// <summary>
    /// SMTP server, e.g. "smtp.office365.com". Empty: emails go to the log (development only).
    /// </summary>
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    /// <summary>
    /// STARTTLS (port 587). System.Net.Mail does not support implicit TLS on port 465.
    /// </summary>
    public bool EnableSsl { get; init; } = true;

    /// <summary>
    /// Empty: the server is used without authentication (e.g. Mailpit).
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Secret: never in appsettings.json; use an environment variable (Email__Smtp__Password) or user secrets.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Sender, e.g. "Market &lt;no-reply@market.local&gt;".
    /// </summary>
    [Required]
    public string From { get; init; } = "Market <no-reply@market.local>";
}
