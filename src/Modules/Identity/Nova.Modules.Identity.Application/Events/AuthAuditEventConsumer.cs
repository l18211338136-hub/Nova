using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain;

namespace Nova.Modules.Identity.Application.Events;

/// <summary>
/// 将认证审计事件持久化到 AuthAuditLog（租户隔离）。
/// 通过 EventBus（MassTransit Mediator）订阅 AuthAuditEvent，实现与业务 Handler 的解耦。
/// </summary>
public class AuthAuditEventConsumer : IConsumer<AuthAuditEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;

    public AuthAuditEventConsumer(IServiceScopeFactory scopeFactory, NovaTenantDbContext tenantDb)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
    }

    public async Task Consume(ConsumeContext<AuthAuditEvent> context)
    {
        var evt = context.Message;

        var tenantInfo = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == evt.TenantId);
        if (tenantInfo is null)
        {
            // 无法定位租户，丢弃审计记录（避免破坏主流程）
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        var db = scope.ServiceProvider.GetRequiredService<IIdentityDbContext>();
        db.AuthAuditLogs.Add(new AuthAuditLog(
            evt.EventType.ToString(),
            evt.TenantId,
            evt.Account,
            evt.UserId,
            evt.Success,
            evt.Reason,
            evt.IpAddress));

        await db.SaveChangesAsync();
    }
}
