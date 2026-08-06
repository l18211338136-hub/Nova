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

        // 1. 捕获标准的独立 AuditedEntity 变更
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

        // 2. 捕获多表关联/聚合根明细表变动（例如 RoleClaim 权限菜单、UserRole 用户角色分配等）
        CaptureAggregateRelationChanges(context, tenantId);
    }

    private void CaptureAggregateRelationChanges(DbContext context, string? tenantId)
    {
        var relationEntries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in relationEntries)
        {
            var typeName = entry.Entity.GetType().Name;

            // RoleClaim (角色绑定的权限或菜单)
            if (typeName.Contains("RoleClaim", StringComparison.OrdinalIgnoreCase))
            {
                var roleIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("RoleId", StringComparison.OrdinalIgnoreCase));
                var claimTypeProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("ClaimType", StringComparison.OrdinalIgnoreCase));
                var claimValueProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("ClaimValue", StringComparison.OrdinalIgnoreCase));

                var roleId = roleIdProp?.CurrentValue?.ToString() ?? roleIdProp?.OriginalValue?.ToString();
                var claimType = claimTypeProp?.CurrentValue?.ToString() ?? claimTypeProp?.OriginalValue?.ToString() ?? "Claim";
                var claimValue = claimValueProp?.CurrentValue?.ToString() ?? claimValueProp?.OriginalValue?.ToString();

                if (!string.IsNullOrEmpty(roleId) && !string.IsNullOrEmpty(claimValue))
                {
                    var log = EntityChangeLog.Create("Role", roleId, "Modified", _currentUser.Id, _currentUser.Name, tenantId);
                    var propDisplayName = claimType.Equals("Permission", StringComparison.OrdinalIgnoreCase) ? "权限 (Permission)" :
                                         claimType.Equals("Menu", StringComparison.OrdinalIgnoreCase) ? "菜单 (Menu)" : claimType;

                    if (entry.State == EntityState.Added)
                    {
                        log.AddPropertyChange(claimType, null, claimValue, propDisplayName);
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        log.AddPropertyChange(claimType, claimValue, null, propDisplayName);
                    }

                    _entityChangeChannel?.WriteAsync(log);
                }
            }
            // UserRole (用户分配的角色)
            else if (typeName.Contains("UserRole", StringComparison.OrdinalIgnoreCase))
            {
                var userIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase));
                var roleIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("RoleId", StringComparison.OrdinalIgnoreCase));

                var userId = userIdProp?.CurrentValue?.ToString() ?? userIdProp?.OriginalValue?.ToString();
                var roleId = roleIdProp?.CurrentValue?.ToString() ?? roleIdProp?.OriginalValue?.ToString();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(roleId))
                {
                    var log = EntityChangeLog.Create("User", userId, "Modified", _currentUser.Id, _currentUser.Name, tenantId);
                    var propDisplayName = "用户角色 (UserRole)";

                    if (entry.State == EntityState.Added)
                    {
                        log.AddPropertyChange("UserRole", null, roleId, propDisplayName);
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        log.AddPropertyChange("UserRole", roleId, null, propDisplayName);
                    }

                    _entityChangeChannel?.WriteAsync(log);
                }
            }
        }
    }
}
