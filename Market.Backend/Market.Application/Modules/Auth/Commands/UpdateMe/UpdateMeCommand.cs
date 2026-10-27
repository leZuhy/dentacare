namespace Market.Application.Modules.Auth.Commands.UpdateMe;

/// <summary>
/// Changes the profile of the logged-in user. Email, password and roles are not changed here.
/// </summary>
public sealed class UpdateMeCommand : IRequest
{
    public string Firstname { get; init; } = string.Empty;

    public string Lastname { get; init; } = string.Empty;

    /// <summary>
    /// Street and house number; empty or null removes it. (optional)
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// City; empty or null removes it. (optional)
    /// </summary>
    public string? City { get; init; }
}
