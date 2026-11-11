using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nova.Framework.MultiTenancy;

public class TenantDatabaseMigrator : ITenantDatabaseMigrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMultiTenantStore<NovaTenantInfo> _tenantStore;
    private readonly ILogger<TenantDatabaseMigrator> _logger;

    public TenantDatabaseMigrator(
        IServiceScopeFactory scopeFactory,
        IMultiTenantStore<NovaTenantInfo> tenantStore,
        ILogger<TenantDatabaseMigrator> logger)
    {
        _scopeFactory = scopeFactory;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task MigrateAndSeedTenantAsync(NovaTenantInfo tenant, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database migration and seeding for tenant '{TenantId}' ({TenantName})...", tenant.Id, tenant.Name);

        using var scope = _scopeFactory.CreateScope();
        var contextSetter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        contextSetter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenant);

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>().ToList();

        // 第一阶段：先对所有模块执行 MigrateAsync，确保 audit、Outbox 等基础表在任何 SeedAsync 写入数据前已创建完毕
        foreach (var initializer in initializers)
        {
            try
            {
                _logger.LogInformation("Migrating DbInitializer '{InitializerType}' for tenant '{TenantId}'...", initializer.GetType().Name, tenant.Id);
                await initializer.MigrateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate DbInitializer '{InitializerType}' for tenant '{TenantId}'.", initializer.GetType().Name, tenant.Id);
                throw;
            }
        }

        // 第二阶段：所有表结构就绪后，再执行 SeedAsync
        foreach (var initializer in initializers)
        {
            try
            {
                _logger.LogInformation("Seeding DbInitializer '{InitializerType}' for tenant '{TenantId}'...", initializer.GetType().Name, tenant.Id);
                await initializer.SeedAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed DbInitializer '{InitializerType}' for tenant '{TenantId}'.", initializer.GetType().Name, tenant.Id);
                throw;
            }
        }

        _logger.LogInformation("Successfully completed database migration and seeding for tenant '{TenantId}'.", tenant.Id);
    }

    public async Task MigrateAndSeedAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantStore.GetAllAsync();
        _logger.LogInformation("Found {TenantCount} active tenants to migrate/seed.", tenants.Count());

        foreach (var tenant in tenants)
        {
            await MigrateAndSeedTenantAsync(tenant, cancellationToken);
        }
    }
}
