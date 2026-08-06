using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain;

// 认证审计日志：记录登录成功/失败、令牌刷新、改密、登出等安全相关事件。
[DisableEntityChangeAuditing]
public class AuthAuditLog : IFullAuditedEntity
{
    private AuthAuditLog()
    {
    }

    public AuthAuditLog(
        string eventType,
        string? account,
        Guid? userId,
        bool success,
        string? reason = null,
        string? ipAddress = null)
    {
        EventType = eventType;
        Account = account;
        UserId = userId;
        Success = success;
        Reason = reason;
        IpAddress = ipAddress;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string EventType { get; set; } = default!;

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
