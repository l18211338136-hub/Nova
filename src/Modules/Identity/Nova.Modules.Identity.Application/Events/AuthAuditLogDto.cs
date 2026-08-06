using System.ComponentModel;

namespace Nova.Modules.Identity.Application.Events;

[Description("认证审计日志")]
public class AuthAuditLogDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Account { get; set; }
    public Guid? UserId { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
