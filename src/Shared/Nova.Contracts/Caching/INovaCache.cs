namespace Nova.Contracts.Caching;

public interface INovaCache
{
    Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken token = default);
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
}
