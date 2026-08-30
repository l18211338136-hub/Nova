using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nova.Framework.Persistence.Outbox;
using Nova.Framework.MultiTenancy;

namespace Nova.Framework.EventBus.Outbox;

public class OutboxProcessorJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public OutboxProcessorJob(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async Task ProcessPendingMessagesAsync()
    {
        using var initScope = _serviceProvider.CreateScope();
        var store = initScope.ServiceProvider.GetService<IMultiTenantStore<NovaTenantInfo>>();

        List<NovaTenantInfo> tenants = new();
        if (store != null)
        {
            var allTenants = await store.GetAllAsync();
            tenants = allTenants.ToList();
        }

        if (tenants.Count == 0)
        {
            // 未扫描到任何租户信息，直接返回
            return;
        }

        foreach (var tenantInfo in tenants)
        {
            try
            {
                using var tenantScope = _serviceProvider.CreateScope();
                
                // 建立租户上下文（与 CleanUnboundFilesJob 保持一致的设计规范）
                var setter = tenantScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);
                
                // 动态构建 DbContextOptions，由于 OutboxDbContext 不是标准的业务 DB，一般不注册进全局 DI
                var connectionString = tenantInfo.ConnectionString ?? _configuration.GetConnectionString("DefaultConnection");
                var optionsBuilder = new DbContextOptionsBuilder<OutboxDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var dbContext = new OutboxDbContext(optionsBuilder.Options, tenantInfo);

                var mediator = tenantScope.ServiceProvider.GetRequiredService<IMediator>();

                // 取前50条未处理且重试次数<3的消息
                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                if (!messages.Any()) continue;

                foreach (var message in messages)
                {
                    try
                    {
                        var eventType = Type.GetType(message.EventType);
                        if (eventType == null)
                        {
                            message.Error = "找不到类型: " + message.EventType;
                            message.ProcessedAt = DateTime.UtcNow;
                            continue;
                        }

                        var domainEvent = JsonSerializer.Deserialize(message.PayloadJson, eventType);
                        
                        // 通过 Mediator 发布事件，并在 Header 注入 OutboxMessageId 用于收件箱去重！
                        if (domainEvent != null)
                        {
                            await mediator.Publish(domainEvent, eventType, Pipe.Execute<PublishContext>(context =>
                            {
                                context.Headers.Set("OutboxMessageId", message.Id.ToString());
                            }));
                        }

                        message.ProcessedAt = DateTime.UtcNow;
                        message.Error = null;
                    }
                    catch (Exception ex)
                    {
                        message.Error = ex.ToString();
                        message.RetryCount++;
                        
                        if (message.RetryCount >= 3)
                        {
                            message.ProcessedAt = DateTime.UtcNow; 
                            message.Error = "达到最大重试次数: " + message.Error;
                        }
                    }
                }

                // 自动清理机制：删除7天前成功处理的消息
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var oldMessages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt != null && m.ProcessedAt < sevenDaysAgo)
                    .Take(100)
                    .ToListAsync();

                if (oldMessages.Any())
                {
                    dbContext.OutboxMessages.RemoveRange(oldMessages);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Outbox processing failed for tenant {tenantInfo.Id}: {ex.Message}");
            }
        }
    }
}
