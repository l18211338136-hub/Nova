using System.Threading.Channels;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Domain.Auditing;

namespace Nova.Framework.Persistence.Auditing;

public class EntityChangeChannel : IEntityChangeChannel, ISingletonDependency
{
    private readonly Channel<EntityChangeLog> _channel = Channel.CreateUnbounded<EntityChangeLog>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask WriteAsync(EntityChangeLog changeLog, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(changeLog, cancellationToken);
    }

    public IAsyncEnumerable<EntityChangeLog> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
