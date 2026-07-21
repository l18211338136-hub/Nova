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

    public async Task MigrateTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantStore.GetAsync(tenantId).ConfigureAwait(false);
        if (tenant == null)
        {
            throw new Exception($"Tenant {tenantId} not found.");
        }

        using var scope = _serviceProvider.CreateScope();

        // 切换到目标租户的上下文
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new ManualTenantContext { TenantInfo = tenant };

        // 解析所有的 IDbInitializer，并在新租户的上下文中自动建库和填充数据
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();

        foreach (var initializer in initializers)
        {
            await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
            await initializer.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
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
