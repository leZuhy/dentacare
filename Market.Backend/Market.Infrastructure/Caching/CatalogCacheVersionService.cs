using Market.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;

namespace Market.Infrastructure.Caching;

/// <summary>
/// <see cref="ICatalogCacheVersionService"/> that keeps the version number in <see cref="IDistributedCache"/>
/// (Redis or memory), without expiration.
/// <para>
/// Read + write is not atomic: two simultaneous bumps can produce the same number. For cache invalidation
/// that is acceptable (the version still changes). With Redis, an atomic <c>INCR</c> through
/// StackExchange.Redis <c>IConnectionMultiplexer</c> would avoid it.
/// </para>
/// </summary>
public sealed class CatalogCacheVersionService(IDistributedCache cache) : ICatalogCacheVersionService
{
    private const string VersionKey = "catalog:version";
    private const long InitialVersion = 1;

    public async Task<long> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(VersionKey, cancellationToken);

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version)
            ? version
            : InitialVersion;
    }

    public async Task<long> BumpVersionAsync(CancellationToken cancellationToken = default)
    {
        var newVersion = await GetVersionAsync(cancellationToken) + 1;

        await cache.SetStringAsync(
            VersionKey,
            newVersion.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions(), // no expiration
            cancellationToken);

        return newVersion;
    }
}
