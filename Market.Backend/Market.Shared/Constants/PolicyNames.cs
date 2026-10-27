namespace Market.Shared.Constants;

/// <summary>
/// Names of the authorization policies registered in Market.API (see <c>AddAuthorization</c>).
/// Use them with <c>[Authorize(Policy = PolicyNames.Admin)]</c> instead of repeating string literals.
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Only users whose access token contains the claim <c>is_admin = "true"</c>.
    /// </summary>
    public const string Admin = "Admin";
}
