using Microsoft.AspNetCore.SignalR;
using Nova.Contracts.Notification;
using Nova.Modules.Notification.Infrastructure.Hubs;
using Nova.Modules.Notification.Domain;

namespace Nova.Modules.Notification.Infrastructure;

public class SystemNotificationService : ISystemNotificationService
{
    private readonly INotificationDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public SystemNotificationService(
        INotificationDbContext dbContext,
        IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<Guid> SendToUserAsync(
        Guid receiverUserId, 
        string eventCode, 
        string title, 
        string content, 
        NotificationType notificationType = NotificationType.Info, 
        string? referenceId = null, 
        string? payloadJson = null, 
        CancellationToken cancellationToken = default)
    {
        // 1. 持久化到数据库
        var notification = new SystemNotification
        {
            ReceiverUserId = receiverUserId,
            EventCode = eventCode,
            Title = title,
            Content = content,
            NotificationType = notificationType,
            ReferenceId = referenceId,
            PayloadJson = payloadJson,
            IsRead = false
        };

        _dbContext.SystemNotifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 2. 尝试通过 SignalR 实时推送到当前在线用户的专属群组
        var groupName = $"User_{receiverUserId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
        {
            Id = notification.Id,
            EventCode = notification.EventCode,
            Title = notification.Title,
            Content = notification.Content,
            NotificationType = notification.NotificationType.ToString(), // 发给前端时转为字符串
            ReferenceId = notification.ReferenceId,
            PayloadJson = notification.PayloadJson,
            CreatedAt = notification.CreatedAt
        }, cancellationToken);

        return notification.Id;
    }
}
