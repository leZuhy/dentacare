using Market.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;

namespace Market.Infrastructure.Email;

/// <summary>
/// Development "email sender": nothing is sent, the email is written to the backend log (console and Logs folder)
/// with the prefix [DEV EMAIL], e.g. so the "forgot password" link can be opened without an SMTP server.
/// <para>
/// Used when "Email:Smtp:Host" is empty. Writing an email to the log is acceptable ONLY because this is a
/// development sink: the log then contains the reset link, i.e. a secret that allows changing the password.
/// That is why the application refuses to start with an empty SMTP host outside Development, Testing and
/// IntegrationTests (see Market.Infrastructure DependencyInjection).
/// </para>
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject}{NewLine}{Body}",
            to, subject, Environment.NewLine, ToPlainText(htmlBody));

        return Task.CompletedTask;
    }

    /// <summary>
    /// HTML as readable text: tags removed and entities decoded, so the link in the log can be copied as is
    /// ("&amp;amp;" in the HTML is "&amp;" in the real link).
    /// </summary>
    private static string ToPlainText(string html)
    {
        var text = Regex.Replace(html, "<[^>]+>", string.Empty);
        return WebUtility.HtmlDecode(text).Trim();
    }
}
