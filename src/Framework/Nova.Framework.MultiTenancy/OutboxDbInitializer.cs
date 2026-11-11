using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Framework.Persistence.Outbox;

namespace Nova.Framework.MultiTenancy;

/// <summary>
/// Outbox / Inbox 事务消息表的租户库初始化器。
/// 当租户使用独立连接串（如散户共享库 RetailConnection 或独立租户库）时，
/// 确保 OutboxMessages 与 InboxMessages 表在该库中完成迁移建表。
/// </summary>
public class OutboxDbInitializer : IDbInitializer
{
    private readonly OutboxDbContext _dbContext;
    private readonly ILogger<OutboxDbInitializer> _logger;

    public OutboxDbInitializer(OutboxDbContext dbContext, ILogger<OutboxDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogInformation("Applying EF Core migrations for OutboxDbContext...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

