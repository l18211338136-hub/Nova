using Medallion.Threading.Redis;
using Nova.Contracts.DistributedLock;
using StackExchange.Redis;

namespace Nova.Framework.Infrastructure.DistributedLock;

public class RedisDistributedLockHandle : IDistributedLockHandle
{
    private readonly Medallion.Threading.IDistributedSynchronizationHandle _handle;

    public RedisDistributedLockHandle(string key, Medallion.Threading.IDistributedSynchronizationHandle handle)
    {
        Key = key;
        _handle = handle;
    }

    public string Key { get; }

    public void Dispose()
    {
        _handle.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _handle.DisposeAsync();
    }
}

public class RedisDistributedLockProvider : IDistributedLockProvider
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IDistributedLockHandle?> TryAcquireLockAsync(
        string key,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        var redisLock = new RedisDistributedLock(key, _redis.GetDatabase());
        var handle = await redisLock.TryAcquireAsync(timeout, cancellationToken);
        if (handle == null) return null;

        return new RedisDistributedLockHandle(key, handle);
    }

    public async Task<IDistributedLockHandle> AcquireLockAsync(
        string key,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var handle = await TryAcquireLockAsync(key, effectiveTimeout, cancellationToken);
        if (handle == null)
        {
            throw new TimeoutException($"Failed to acquire distributed lock for key '{key}' within timeout {effectiveTimeout.TotalSeconds}s.");
        }

        return handle;
    }
}
