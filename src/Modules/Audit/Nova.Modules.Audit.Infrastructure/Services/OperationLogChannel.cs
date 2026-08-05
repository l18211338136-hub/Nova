using System.Threading.Channels;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Web.Logging;

namespace Nova.Modules.Audit.Infrastructure.Services;

public record OperationLogQueueItem(OperationLogRequest Request, string? TenantId);

public class OperationLogChannel : IOperationLogChannel, ISingletonDependency
{
    private readonly Channel<OperationLogQueueItem> _channel = Channel.CreateUnbounded<OperationLogQueueItem>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    public ValueTask WriteAsync(OperationLogRequest request, string? tenantId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(new OperationLogQueueItem(request, tenantId), cancellationToken);
    }

    public IAsyncEnumerable<OperationLogQueueItem> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
