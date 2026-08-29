using Nova.Contracts.Notification;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Notification.Domain;

public class SystemNotification : FullAuditedEntity<Guid>
{
    // 接收人 ID
    public Guid ReceiverUserId { get; set; }
    
    // 标题 (兜底显示)
    public string Title { get; set; } = string.Empty;
    
    // 内容 (兜底显示)
    public string Content { get; set; } = string.Empty;
    
    // 通知类型 (Info, Success, Warning, Error)
    public NotificationType NotificationType { get; set; } = NotificationType.Info;
    
    // 事件信息码 (Event Code，供前端做多语言或业务跳转)
    public string EventCode { get; set; } = string.Empty;
    
    // 关联的业务主体 ID (如审批单 ID, 文件 ID 等)
    public string? ReferenceId { get; set; }
    
    // 序列化的 JSON Payload，用于传递额外参数
    public string? PayloadJson { get; set; }
    
    // 是否已读
    public bool IsRead { get; set; } = false;
    
    // 阅读时间
    public DateTimeOffset? ReadAt { get; set; }
}
