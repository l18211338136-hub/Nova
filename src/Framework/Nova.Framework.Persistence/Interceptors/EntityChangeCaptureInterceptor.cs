using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Security;
using Nova.Framework.Domain.Auditing;

namespace Nova.Framework.Persistence.Interceptors;

public class EntityChangeCaptureInterceptor : SaveChangesInterceptor, IScopedDependency
{
    private static readonly HashSet<string> IgnoredAuditProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "ModifiedAt", "ModifiedBy", "CreatedAt", "CreatedBy"
    };

    private readonly ICurrentUser _currentUser;
    private readonly IEntityChangeChannel? _entityChangeChannel;

    public EntityChangeCaptureInterceptor(ICurrentUser currentUser, IEntityChangeChannel? entityChangeChannel = null)
    {
        _currentUser = currentUser;
        _entityChangeChannel = entityChangeChannel;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureEntityChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureEntityChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureEntityChanges(DbContext? context)
    {
        if (context == null || _entityChangeChannel == null) return;

        // 如果发生的 DbContext 本身就是 AuditDbContext 正在写入 EntityChangeLog，直接跳过防递归
        if (context.GetType().Name.Equals("AuditDbContext", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 从当前 DbContext 的 TenantInfo 提取真实的多租户 ID
        string? tenantId = null;
        try
        {
            var tenantInfoProp = context.GetType().GetProperty("TenantInfo");
            if (tenantInfoProp != null)
            {
                var tenantObj = tenantInfoProp.GetValue(context);
                var idProp = tenantObj?.GetType().GetProperty("Identifier") ?? tenantObj?.GetType().GetProperty("Id");
                tenantId = idProp?.GetValue(tenantObj)?.ToString();
            }
        }
        catch { }

        // 使用 [DisableEntityChangeAuditing] 特性实现纯粹声明式过滤
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditedEntity &&
                        !e.Entity.GetType().IsDefined(typeof(DisableEntityChangeAuditingAttribute), true) &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
            var entityId = idProp?.CurrentValue?.ToString() ?? idProp?.OriginalValue?.ToString() ?? Guid.NewGuid().ToString();

            var changeType = entry.State.ToString();
            var log = EntityChangeLog.Create(entityType, entityId, changeType, _currentUser.Id, _currentUser.Name, tenantId);

            if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey() || prop.Metadata.IsConcurrencyToken || IgnoredAuditProperties.Contains(prop.Metadata.Name)) continue;

                    var orig = prop.OriginalValue?.ToString();
                    var curr = prop.CurrentValue?.ToString();

                    if (!Equals(orig, curr))
                    {
                        log.AddPropertyChange(prop.Metadata.Name, orig, curr);
                    }
                }

                if (log.PropertyChanges.Count > 0)
                {
                    _entityChangeChannel.WriteAsync(log);
                }
            }
            else if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey() || prop.Metadata.IsConcurrencyToken || IgnoredAuditProperties.Contains(prop.Metadata.Name)) continue;
                    var curr = prop.CurrentValue?.ToString();
                    if (!string.IsNullOrEmpty(curr))
                    {
                        log.AddPropertyChange(prop.Metadata.Name, null, curr);
                    }
                }
                if (log.PropertyChanges.Count > 0)
                {
                    _entityChangeChannel.WriteAsync(log);
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey() || prop.Metadata.IsConcurrencyToken || IgnoredAuditProperties.Contains(prop.Metadata.Name)) continue;
                    var orig = prop.OriginalValue?.ToString();
                    if (!string.IsNullOrEmpty(orig))
                    {
                        log.AddPropertyChange(prop.Metadata.Name, orig, null);
                    }
                }
                if (log.PropertyChanges.Count > 0)
                {
                    _entityChangeChannel.WriteAsync(log);
                }
            }
        }
    }
}
