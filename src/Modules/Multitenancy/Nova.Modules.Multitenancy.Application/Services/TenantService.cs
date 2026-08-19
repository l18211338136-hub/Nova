using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.MultiTenancy;

namespace Nova.Modules.Multitenancy.Application.Services;

public class TenantService : ITenantService, IScopedDependency
{
    private readonly IMultiTenantStore<NovaTenantInfo> _tenantStore;
    private readonly IServiceProvider _serviceProvider;

    public TenantService(IMultiTenantStore<NovaTenantInfo> tenantStore, IServiceProvider serviceProvider)
    {
        _tenantStore = tenantStore;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> CreateTenantAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, CancellationToken cancellationToken)
    {
        var tenant = new NovaTenantInfo
        {
            Id = id,
            Identifier = id,
            Name = name,
            ConnectionString = connectionString,
            AdminEmail = adminEmail,
            Issuer = issuer,
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };

        await _tenantStore.AddAsync(tenant).ConfigureAwait(false);

        return tenant.Id;
    }

    public async Task UpdateTenantAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, bool isActive, DateTime validUpto, CancellationToken cancellationToken)
    {
        var tenant = await _tenantStore.GetAsync(id).ConfigureAwait(false);
        if (tenant == null)
            throw new Exception($"Tenant {id} not found.");

        tenant.Name = name;
        tenant.ConnectionString = connectionString;
        tenant.AdminEmail = adminEmail;
        tenant.Issuer = issuer;
        tenant.IsActive = isActive;
        tenant.ValidUpto = validUpto;

        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
    }

    public async Task DeleteTenantAsync(string id, CancellationToken cancellationToken)
    {
        await _tenantStore.RemoveAsync(id).ConfigureAwait(false);
    }

    public async Task MigrateTenantAsync(string tenantId, string? adminPassword = null, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantStore.GetAsync(tenantId).ConfigureAwait(false);
        if (tenant == null)
        {
            throw new Exception($"Tenant {tenantId} not found.");
        }

        // 绑定管理密码
        if (adminPassword != null)
        {
            tenant.AdminPassword = adminPassword;
        }

        var migrator = _serviceProvider.GetRequiredService<ITenantDatabaseMigrator>();
        await migrator.MigrateAndSeedTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 用于在后台任务或特定作用域中手动指定和切换租户上下文
/// </summary>
public class ManualTenantContext : IMultiTenantContext<NovaTenantInfo>
{
    public NovaTenantInfo? TenantInfo { get; set; }
    ITenantInfo? IMultiTenantContext.TenantInfo { get => TenantInfo; init => TenantInfo = (NovaTenantInfo?)value; }
    public StrategyInfo? StrategyInfo { get; init; }
    public StoreInfo<NovaTenantInfo>? StoreInfo { get; set; }
    public bool IsResolved => true;
}
