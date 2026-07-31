using System.Threading;
using System.Threading.Tasks;

namespace Nova.Framework.Domain.SeedWork;

/// <summary>
/// 进程内领域事件分发器（EventBus 抽象）。
/// 实现可基于 MassTransit Mediator、真实消息队列等；领域/应用层只依赖此抽象。
/// </summary>
public interface IDomainEventDispatcher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
