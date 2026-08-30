using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.Persistence.Outbox;

namespace Nova.Framework.EventBus.Outbox;

public class InboxFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly DbContextOptions<OutboxDbContext> _options;

    public InboxFilter(DbContextOptions<OutboxDbContext> options)
    {
        _options = options;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // 如果没有 OutboxMessageId 请求头，说明不是通过发件箱发出的，直接放行
        if (!context.Headers.TryGetHeader("OutboxMessageId", out var outboxIdObj) ||
            !Guid.TryParse(outboxIdObj?.ToString(), out var messageId))
        {
            await next.Send(context);
            return;
        }

        var consumerName = context.ReceiveContext.InputAddress.AbsolutePath ?? "UnknownConsumer";

        // 创建短生命周期的 OutboxDbContext，不需要租户信息也可以，
        // 因为 InboxMessages 是通过全局 Id 和 ConsumerName 幂等，如果开启了多租户，
        // 则最好能从上下文中获取 TenantInfo（这里为演示简化处理）
        using var dbContext = new OutboxDbContext(_options, null!); // 强制忽略租户以便全库去重，或注入 ITenantInfo

        var exists = await dbContext.InboxMessages.AnyAsync(m => m.Id == messageId && m.ConsumerName == consumerName);
        if (exists)
        {
            // 已处理过，直接幂等返回，不抛异常
            return;
        }

        // 继续执行消费者业务逻辑
        await next.Send(context);

        // 如果业务执行成功（没有抛出异常），则记录到收件箱
        var inboxMessage = new InboxMessage
        {
            Id = messageId,
            ConsumerName = consumerName,
            ProcessedAt = DateTime.UtcNow
        };

        dbContext.InboxMessages.Add(inboxMessage);
        await dbContext.SaveChangesAsync();
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inbox");
    }
}
