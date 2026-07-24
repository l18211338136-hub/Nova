using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace Nova.Framework.Infrastructure.Caching;

public class NovaCacheService : INovaCache, ISingletonDependency
{
    private readonly IFusionCache _fusionCache;

    public NovaCacheService(IFusionCache fusionCache)
    {
        _fusionCache = fusionCache;
    }

    public Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken token = default)
    {
        var options = expiration.HasValue ? new FusionCacheEntryOptions(expiration.Value) : null;
        return _fusionCache.GetOrSetAsync<T>(key, async (ctx, ct) => await factory(ct), options, token).AsTask()!;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        return _fusionCache.GetOrDefaultAsync<T?>(key, default, token: token).AsTask();
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken token = default)
    {
        var options = expiration.HasValue ? new FusionCacheEntryOptions(expiration.Value) : null;
        return _fusionCache.SetAsync(key, value, options, token: token).AsTask();
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return _fusionCache.RemoveAsync(key, token: token).AsTask();
    }
}
