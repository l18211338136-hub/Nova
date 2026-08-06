namespace Nova.Contracts.DistributedLock;

public interface IDistributedLockProvider
{
    /// <summary>
    /// 尝试获取分布式锁。若在超时时间内未能获取锁，则返回 null。
    /// </summary>
    Task<IDistributedLockHandle?> TryAcquireLockAsync(
        string key,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分布式锁。若在超时时间内未能获取锁，则抛出 TimeoutException。
    /// </summary>
    Task<IDistributedLockHandle> AcquireLockAsync(
        string key,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
