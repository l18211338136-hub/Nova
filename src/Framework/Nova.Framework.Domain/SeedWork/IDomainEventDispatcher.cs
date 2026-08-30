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
    
    /// <summary>
    /// 支持事务性发件箱的发布方法。
    /// 传入当前的 DbContext (作为 transactionContext)，将事件保存至独立发件箱中，并与业务 DbContext 共享物理事务提交。
    /// </summary>
    Task PublishAsync<TTransactionContext>(IDomainEvent domainEvent, TTransactionContext transactionContext, CancellationToken cancellationToken = default) where TTransactionContext : class;
}
