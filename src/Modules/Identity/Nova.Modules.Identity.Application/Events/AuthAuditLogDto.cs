using System.ComponentModel;

namespace Nova.Modules.Identity.Application.Events;

/// <summary>
/// 认证审计日志数据传输对象。用于分页查询登录/刷新令牌/改密/登出等安全事件。
/// </summary>
[Description("认证审计日志")]
public class AuthAuditLogDto
{
    /// <summary>日志唯一标识</summary>
    public Guid Id { get; set; }

    /// <summary>事件类型（AuthAuditEventType 的枚举名，如 LoginSuccess / LoginFailed / TokenRefreshed / PasswordChanged / Logout）</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>账号（邮箱或用户名）</summary>
    public string? Account { get; set; }

    /// <summary>用户标识</summary>
    public Guid? UserId { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>失败原因（成功时为空）</summary>
    public string? Reason { get; set; }

    /// <summary>客户端 IP 地址</summary>
    public string? IpAddress { get; set; }

    /// <summary>事件发生时间（UTC）</summary>
    public DateTime OccurredOn { get; set; }

    /// <summary>记录写入时间（带时区偏移）</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
