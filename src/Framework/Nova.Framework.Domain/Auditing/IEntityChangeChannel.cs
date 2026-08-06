namespace Nova.Framework.Domain.Auditing;

public interface IEntityChangeChannel
{
    ValueTask WriteAsync(EntityChangeLog changeLog, CancellationToken cancellationToken = default);
    IAsyncEnumerable<EntityChangeLog> ReadAllAsync(CancellationToken cancellationToken = default);
}
