using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Framework.MultiTenancy;

namespace Nova.Modules.Notification.Infrastructure;

/// <summary>
/// Notification 模块的租户库初始化器。
/// 确保独立租户库 / 散户库（RetailConnection）中创建 notification.SystemNotifications 表。
/// </summary>
public class NotificationDbInitializer : IDbInitializer
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<NotificationDbInitializer> _logger;

    public NotificationDbInitializer(NotificationDbContext dbContext, ILogger<NotificationDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogInformation("Applying EF Core migrations for NotificationDbContext...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

