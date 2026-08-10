using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Storage;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Storage.Application.Database;

namespace Nova.Modules.Storage.Infrastructure.Jobs;

/// <summary>
/// 未绑定文件自动清理回收 Job。
/// 支持完整多租户模式（同时兼容 共享数据库单表租户隔离 与 独立数据库-per-tenant 数据库隔离）。
/// </summary>
public class CleanUnboundFilesJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanUnboundFilesJob> _logger;

    public CleanUnboundFilesJob(IServiceScopeFactory scopeFactory, ILogger<CleanUnboundFilesJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Storage.GC] 启动多租户未绑定文件自动清理回收扫描...");

        using var initScope = _scopeFactory.CreateScope();
        var store = initScope.ServiceProvider.GetService<IMultiTenantStore<NovaTenantInfo>>();

        List<NovaTenantInfo> tenants = new();
        if (store != null)
        {
            var allTenants = await store.GetAllAsync();
            tenants = allTenants.ToList();
        }

        if (tenants.Count == 0)
        {
            _logger.LogInformation("[Storage.GC] 未扫描到任何有效租户信息。");
            return;
        }

        foreach (var tenantInfo in tenants)
        {
            try
            {
                await CleanForTenantAsync(tenantInfo, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Storage.GC] 租户 '{TenantId}' ({TenantName}) 执行未绑定文件清理失败。", tenantInfo.Id, tenantInfo.Name);
            }
        }

        _logger.LogInformation("[Storage.GC] 全局多租户未绑定文件清理扫描完成。");
    }

    private async Task CleanForTenantAsync(NovaTenantInfo tenantInfo, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        
        // 建立租户上下文，Finbuckle 会根据租户配置自动切换连接字符串（独立库隔离）或注入 TenantId 过滤器（共享库隔离）
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        var db = scope.ServiceProvider.GetRequiredService<IStorageDbContext>();
        var storageProvider = scope.ServiceProvider.GetRequiredService<IStorageProvider>();

        var threshold = DateTimeOffset.UtcNow.AddHours(-24);

        // 1. 查找当前租户下创建超过 24 小时未绑定任何 Attachments 的物理文件 ID
        var unboundFileIds = await db.FileObjects
            .Where(f => f.CreatedAt <= threshold)
            .Where(f => !db.Attachments.Any(a => a.FileId == f.Id))
            .OrderBy(f => f.CreatedAt)
            .Select(f => f.Id)
            .Take(100) // 每次 Batch 处理 100 个
            .ToListAsync(cancellationToken);

        if (unboundFileIds.Count == 0)
        {
            return;
        }

        var unboundFiles = await db.FileObjects
            .Where(f => unboundFileIds.Contains(f.Id))
            .ToListAsync(cancellationToken);

        int deletedCount = 0;
        foreach (var file in unboundFiles)
        {
            // 校验是否有相同 FileHash 且受引用的其他 FileObject 共享同路径物理文件
            var samePathSharedCount = await db.FileObjects
                .AsNoTracking()
                .CountAsync(x => x.FileKey == file.FileKey && x.Id != file.Id, cancellationToken);

            if (samePathSharedCount == 0)
            {
                // 无其他记录共享同路径，可以安全物理删除
                await storageProvider.DeleteAsync(file.FileKey, file.BucketName, cancellationToken);
            }

            db.FileObjects.Remove(file);
            deletedCount++;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[Storage.GC] 租户 '{TenantId}' ({TenantName}) 成功清理了 {Count} 个过期未绑定文件及其存储资源。", tenantInfo.Id, tenantInfo.Name, deletedCount);
    }
}
