using System.ComponentModel.DataAnnotations;

namespace Market.Shared.Options;

/// <summary>"ConnectionStrings" section.</summary>
public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required] public string Main { get; init; } = default!;

    /// <summary>
    /// Redis connection string, e.g. "localhost:6379". (optional)
    /// Empty (default): the distributed cache is kept in memory and no Redis server is needed.
    /// </summary>
    public string? Redis { get; init; }
}
