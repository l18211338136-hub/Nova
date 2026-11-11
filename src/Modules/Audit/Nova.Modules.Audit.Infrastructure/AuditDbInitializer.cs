using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Audit.Infrastructure.Persistence;

namespace Nova.Modules.Audit.Infrastructure;

/// <summary>
/// Audit 模块的租户库初始化器。
/// 启动期的自动迁移只作用于 DefaultConnection；散户注册 / 新建租户时若租户使用独立连接串
/// （如 RetailConnection），必须由 IDbInitializer 在该租户库上建立 audit schema 与表，
/// 否则 LogProcessor 写入 audit.OperationLogs / audit.EntityChangeLogs 时会报 42P01。
/// </summary>
public class AuditDbInitializer : IDbInitializer
{
    private readonly AuditDbContext _dbContext;
    private readonly ILogger<AuditDbInitializer> _logger;

    public AuditDbInitializer(AuditDbContext dbContext, ILogger<AuditDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogInformation("Applying EF Core migrations for AuditDbContext...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    // 审计模块无种子数据
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

