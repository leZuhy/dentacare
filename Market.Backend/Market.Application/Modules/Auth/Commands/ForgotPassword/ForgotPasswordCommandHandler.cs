using Market.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Market.Application.Modules.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IAppDbContext ctx,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    TimeProvider timeProvider,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand>
{
    /// <summary>
    /// How long the link from the email is valid.
    /// </summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 32 random bytes = 256 bits, impossible to guess.
    /// </summary>
    private const int TokenByteCount = 32;

    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await ctx.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsEnabled, ct);

        // Unknown or disabled account: nothing happens, and the client gets the same 204 as for an existing one
        // (otherwise anybody could check which emails are registered).
        if (user is null)
            return;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Only the newest link works: earlier unused tokens of this user are cancelled (soft delete).
        var previousTokens = await ctx.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
            .ToListAsync(ct);

        ctx.PasswordResetTokens.RemoveRange(previousTokens);

        // The raw token goes only into the email; the database stores its hash (like refresh tokens).
        var token = SecureToken.Generate(TokenByteCount);

        ctx.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            UserId = user.Id,
            TokenHash = SecureToken.Hash(token),
            ExpiresAtUtc = nowUtc.Add(TokenLifetime)
        });

        await ctx.SaveChangesAsync(ct);

        var link = BuildResetLink(user.Email, token);

        try
        {
            await emailSender.SendAsync(user.Email, "Market – promjena lozinke", BuildEmailBody(user.Firstname, link), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An email error must not change the response either (it would reveal that the account exists).
            // It is logged for the administrator; the user can request a new link.
            logger.LogError(ex, "Password reset email could not be sent to user {UserId}.", user.Id);
        }
    }

    /// <summary>
    /// <c>{Frontend:BaseUrl}/auth/reset-password?email=...&amp;token=...</c> — the Angular page that sends
    /// email, token and the new password to <c>POST /api/auth/reset-password</c>.
    /// </summary>
    private string BuildResetLink(string email, string token)
    {
        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/auth/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    /// <summary>
    /// Simple HTML email. Values are HTML-encoded: the first name is entered by the user,
    /// and "&amp;" in the link must be written as "&amp;amp;" inside HTML.
    /// </summary>
    private static string BuildEmailBody(string firstname, string link)
    {
        var name = WebUtility.HtmlEncode(firstname);
        var href = WebUtility.HtmlEncode(link);

        return $"""
            <p>Pozdrav {name},</p>
            <p>primili smo zahtjev za promjenu lozinke Vašeg Market naloga. Novu lozinku možete postaviti na sljedećem linku:</p>
            <p><a href="{href}">{href}</a></p>
            <p>Link važi {TokenLifetime.TotalMinutes} minuta i može se iskoristiti samo jednom.
            Ako niste tražili promjenu lozinke, zanemarite ovaj email – Vaša lozinka ostaje ista.</p>
            <p>Market</p>
            """;
    }
}
