using Nova.Contracts.DependencyInjection;

namespace Nova.Framework.MultiTenancy;

/// <summary>
/// 多租户数据库自动迁移与播种服务契约
/// </summary>
public interface ITenantDatabaseMigrator : IScopedDependency
{
    /// <summary>
    /// 为指定租户自动应用 EF Core 迁移并进行初始化播种
    /// </summary>
    Task MigrateAndSeedTenantAsync(NovaTenantInfo tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 自动为系统中的所有活动租户运行迁移与播种
    /// </summary>
    Task MigrateAndSeedAllTenantsAsync(CancellationToken cancellationToken = default);
}
