namespace Market.Application.Abstractions;

/// <summary>
/// Sends an email. Handlers build the content; the implementation only delivers it.
/// <para>
/// Registered in Market.Infrastructure by configuration ("Email:Smtp:Host"):
/// SmtpEmailSender when a host is set, otherwise LoggingEmailSender, which only writes the email
/// to the backend log (allowed only in Development, Testing and IntegrationTests).
/// </para>
/// </summary>
public interface IEmailSender
{
    /// <param name="to">Recipient address.</param>
    /// <param name="subject">Subject.</param>
    /// <param name="htmlBody">Body as HTML. Values entered by users must be HTML-encoded by the caller.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
