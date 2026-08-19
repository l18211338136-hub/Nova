namespace Nova.Framework.EventBus;

/// <summary>
/// 统一事件总线驱动契约 (隔离 进程内事件 / MassTransit / RabbitMQ / Kafka 细节)
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布集成事件
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    /// <summary>
    /// 延迟发布事件
    /// </summary>
    Task PublishDelayedAsync<TEvent>(TEvent @event, TimeSpan delay, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}
