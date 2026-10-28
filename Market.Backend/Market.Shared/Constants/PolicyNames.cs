namespace Market.Shared.Constants;

/// <summary>
/// Names of the authorization policies registered in Market.API (see <c>AddAuthorization</c>).
/// Use them with <c>[Authorize(Policy = PolicyNames.Admin)]</c> instead of repeating string literals.
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Only users with the Admin role.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Only users with the Dentist role.
    /// </summary>
    public const string Dentist = "Dentist";

    /// <summary>
    /// Only users with the Patient role.
    /// </summary>
    public const string Patient = "Patient";
}
