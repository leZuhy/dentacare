using Market.Application.Abstractions;
using System.Collections.Concurrent;

namespace Market.Tests;

/// <summary>
/// <see cref="IEmailSender"/> for integration tests: nothing is sent, every email is kept in memory,
/// so a test can read e.g. the password reset link. Registered by <see cref="CustomWebApplicationFactory{TProgram}"/>.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _sent.Enqueue(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }

    /// <summary>
    /// All emails sent to <paramref name="to"/>, oldest first. Tests share one API instance,
    /// so each test uses its own (random) email address.
    /// </summary>
    public IReadOnlyList<SentEmail> SentTo(string to)
    {
        return _sent.Where(e => string.Equals(e.To, to, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);
