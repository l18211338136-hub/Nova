using Nova.Framework.Domain.SeedWork;

namespace Nova.Modules.Identity.Application.Events;

public enum AuthAuditEventType
{
    LoginSuccess,
    LoginFailed,
    TokenRefreshed,
    PasswordChanged,
    Logout
}

// 认证审计领域事件，由 Handler 通过 IDomainEventDispatcher 发布，由 AuthAuditEventConsumer 持久化
public class AuthAuditEvent : IDomainEvent
{
    public AuthAuditEvent(
        AuthAuditEventType eventType,
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

    public AuthAuditEventType EventType { get; }
    public string TenantId { get; }
    public string? Account { get; }
    public Guid? UserId { get; }
    public bool Success { get; }
    public string? Reason { get; }
    public string? IpAddress { get; }
    public DateTime OccurredOn { get; }
}
