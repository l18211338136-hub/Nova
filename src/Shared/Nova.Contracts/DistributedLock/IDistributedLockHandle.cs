namespace Nova.Contracts.DistributedLock;

public interface IDistributedLockHandle : IAsyncDisposable, IDisposable
{
    string Key { get; }
}
