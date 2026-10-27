using Market.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Market.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> over <see cref="IDistributedCache"/>: Redis when <c>ConnectionStrings:Redis</c>
/// is set, otherwise an in-memory cache (see DependencyInjection). Values are stored as JSON.
/// </summary>
public sealed class CacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<T>(cached, JsonOptions);

        var value = await factory(cancellationToken);

        if (value is not null)
        {
            await cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(value, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }

        return value;
    }
}
