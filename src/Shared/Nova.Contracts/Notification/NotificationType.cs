namespace Nova.Contracts.Notification;

/// <summary>
/// 通知级别/类型
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 普通信息
    /// </summary>
    Info = 0,
    
    /// <summary>
    /// 成功提示
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// 警告提示
    /// </summary>
    Warning = 2,
    
    /// <summary>
    /// 错误提示
    /// </summary>
    Error = 3
}
