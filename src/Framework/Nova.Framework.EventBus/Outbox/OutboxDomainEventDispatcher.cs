using System.Text.Json;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Domain.SeedWork;
using Nova.Framework.Persistence.Outbox;

namespace Nova.Framework.EventBus.Outbox;

/// <summary>
/// 全局领域事件发件箱分发器（已移至 EventBus 层以解耦对 MassTransit 的依赖）。
/// 不再绑定特定的 IdentityDbContext，支持任何传入的 DbContext 共享事务。
/// </summary>
public class OutboxDomainEventDispatcher : IDomainEventDispatcher, ITransientDependency
{
    private readonly IMediator _mediator;

    public OutboxDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // 兼容非事务模式：直接投递（适用于不需要发件箱保证的场景）
        return _mediator.Publish((object)domainEvent!, cancellationToken);
    }

    public async Task PublishAsync<TTransactionContext>(IDomainEvent domainEvent, TTransactionContext transactionContext, CancellationToken cancellationToken = default) where TTransactionContext : class
    {
        var businessDbContext = transactionContext as DbContext;
        if (businessDbContext == null)
        {
            // 如果传入的不是 DbContext，降级为直接发布
            await PublishAsync(domainEvent, cancellationToken);
            return;
        }

        // 1. 获取当前业务 DbContext 的物理连接和事务
        var connection = businessDbContext.Database.GetDbConnection();
        var currentTransaction = businessDbContext.Database.CurrentTransaction?.GetDbTransaction();

        // 2. 动态创建独立的 OutboxDbContext，并共用底层连接
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseNpgsql(connection)
            .Options;
        
        // 动态获取 TenantInfo（如果业务 DbContext 是多租户的）
        var tenantInfo = ((dynamic)businessDbContext).TenantInfo; 
        
        using var outboxDb = new OutboxDbContext(options, tenantInfo);
        
        // 3. 强行加入业务事务
        if (currentTransaction != null)
        {
            outboxDb.Database.UseTransaction(currentTransaction);
        }

        var outboxMessage = new OutboxMessage
        {
            EventType = domainEvent.GetType().AssemblyQualifiedName!,
            PayloadJson = JsonSerializer.Serialize((object)domainEvent)
        };

        outboxDb.OutboxMessages.Add(outboxMessage);
        
        // 4. 保存发件箱，推入业务的大事务中
        await outboxDb.SaveChangesAsync(cancellationToken);
    }
}
