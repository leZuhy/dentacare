using Market.Application.Abstractions;
using Market.Shared.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Market.Infrastructure.Email;

/// <summary>
/// Sends emails through the SMTP server from "Email:Smtp" (used when Host is set).
/// For a local test: Mailpit or Papercut on localhost:1025 (EnableSsl false) shows every sent email in the browser.
/// <para>
/// System.Net.Mail needs no extra package and is enough here. For new production code Microsoft recommends
/// a library such as MailKit (implicit TLS on port 465, OAuth...); only this class would change.
/// </para>
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var smtp = options.Value;

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.From),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = htmlBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl
        };

        // Without a user name the server is used anonymously (e.g. Mailpit, Papercut)
        if (!string.IsNullOrWhiteSpace(smtp.UserName))
            client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);

        await client.SendMailAsync(message, ct);
    }
}
