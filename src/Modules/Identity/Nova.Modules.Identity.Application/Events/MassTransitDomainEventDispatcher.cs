using MassTransit;
using MassTransit.Mediator;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Domain.SeedWork;

namespace Nova.Modules.Identity.Application.Events;

/// <summary>
/// 基于 MassTransit Mediator 的领域事件分发器实现。
/// 通过 IMediator.Publish 将事件投递给所有 IConsumer&lt;TEvent&gt; 订阅者（跨模块解耦）。
/// </summary>
public class MassTransitDomainEventDispatcher : IDomainEventDispatcher, ITransientDependency
{
    private readonly IMediator _mediator;

    public MassTransitDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // MassTransit 按运行时类型路由到对应的 IConsumer<T>
        return _mediator.Publish((object)domainEvent!, cancellationToken);
    }
}
