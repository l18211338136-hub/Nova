using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain;

/// <summary>
/// 认证审计日志。记录登录成功/失败、令牌刷新、改密、登出等安全相关事件。
/// 多租户隔离（按 TenantId），并带软删除审计字段。
/// </summary>
public class AuthAuditLog : IFullAuditedEntity
{
    private AuthAuditLog()
    {
    }

    public AuthAuditLog(
        string eventType,
        string tenantId,
        string? account,
        Guid? userId,
        bool success,
        string? reason = null,
        string? ipAddress = null)
    {
        EventType = eventType;
        TenantId = tenantId;
        Account = account;
        UserId = userId;
        Success = success;
        Reason = reason;
        IpAddress = ipAddress;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>事件类型（AuthAuditEventType 的枚举名）</summary>
    public string EventType { get; set; } = default!;

    /// <summary>所属租户标识（与 Finbuckle 多租户分区一致）</summary>
    public string TenantId { get; set; } = default!;

    public string? Account { get; set; }
    public Guid? UserId { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredOn { get; set; }

    // IFullAuditedEntity
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    // IFullAuditedEntity（审计日志本身不需要这些语义字段，给默认值即可）
    public string? Remarks { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}
