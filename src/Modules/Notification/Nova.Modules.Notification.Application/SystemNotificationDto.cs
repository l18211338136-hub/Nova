using System.Text.Json.Serialization;

namespace Nova.Modules.Notification.Application;

public class SystemNotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string? PayloadJson { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public record MarkNotificationAsReadResult
{
    public bool Success { get; init; }
}

public record MarkAllNotificationsAsReadResult
{
    public bool Success { get; init; }
}

public record SendTestNotificationResult
{
    public Guid NotificationId { get; init; }
}

public record DeleteNotificationResult
{
    public bool Success { get; init; }
}

public record ClearReadNotificationsResult
{
    public bool Success { get; init; }
}
