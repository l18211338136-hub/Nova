namespace Nova.Contracts.Notification;

/// <summary>
/// 跨模块统一发送系统通知的服务
/// </summary>
public interface ISystemNotificationService
{
    /// <summary>
    /// 发送强类型系统通知（带信息码），同时进行持久化并尝试通过 SignalR 实时推送到客户端
    /// </summary>
    /// <param name="receiverUserId">接收人 ID</param>
    /// <param name="eventCode">事件信息码 (用于前端多语言翻译和业务逻辑跳转)</param>
    /// <param name="title">默认标题文本</param>
    /// <param name="content">默认内容文本</param>
    /// <param name="notificationType">通知类型: Info, Success, Warning, Error</param>
    /// <param name="referenceId">关联的业务 ID (如某文档 ID)</param>
    /// <param name="payloadJson">额外的上下文参数 JSON，用于前端动态渲染</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回持久化后的通知 ID</returns>
    Task<Guid> SendToUserAsync(
        Guid receiverUserId, 
        string eventCode, 
        string title, 
        string content, 
        NotificationType notificationType = NotificationType.Info, 
        string? referenceId = null,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);
}
