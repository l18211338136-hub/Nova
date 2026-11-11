using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Storage.Infrastructure.Persistence;

namespace Nova.Modules.Storage.Infrastructure;

/// <summary>
/// Storage 模块的租户库初始化器。
/// 确保独立租户库 / 散户库（RetailConnection）中创建 storage.FileObjects 与 storage.Attachments 表。
/// </summary>
public class StorageDbInitializer : IDbInitializer
{
    private readonly StorageDbContext _dbContext;
    private readonly ILogger<StorageDbInitializer> _logger;

    public StorageDbInitializer(StorageDbContext dbContext, ILogger<StorageDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogInformation("Applying EF Core migrations for StorageDbContext...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

