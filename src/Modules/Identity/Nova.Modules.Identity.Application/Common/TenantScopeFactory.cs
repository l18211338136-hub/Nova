using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Domain;

namespace Nova.Modules.Identity.Application.Common;

/// <summary>
/// Handler 运行在 MassTransit 消费上下文中，拿不到 HttpContext，因此 Finbuckle 的租户上下文不会自动建立。
/// 这里根据命令上由框架注入的 CurrentTenantId 重建一个带租户上下文的 IServiceScope，
/// 之后从该 scope 取出的 UserManager / DbContext 才会命中正确的租户库。
/// </summary>
public static class TenantScopeFactory
{
    /// <summary>
    /// 创建带租户上下文的服务作用域。调用方负责 Dispose（建议 using）。
    /// </summary>
    public static async Task<(IServiceScope Scope, string TenantId)> CreateAsync(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb,
        string? currentTenantId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = string.IsNullOrWhiteSpace(currentTenantId)
            ? NovaIdentityConstants.Tenants.RootTenantId
            : currentTenantId!;

        var tenantInfo = await tenantDb.TenantInfo
            .FirstOrDefaultAsync(t => t.Identifier == tenantId, cancellationToken);

        if (tenantInfo == null)
        {
            throw new NovaValidationException("无法定位租户信息");
        }

        var scope = scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        return (scope, tenantId);
    }
}
