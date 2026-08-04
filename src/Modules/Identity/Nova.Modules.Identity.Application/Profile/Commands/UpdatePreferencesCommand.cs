using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Commands;

/// <summary>
/// 更新当前登录用户的个性化偏好。
/// 设置页的外观 / 通知 / 显示是三个独立表单，因此所有字段均可空：
/// 传 null 表示「本次不修改该项」，从而支持局部更新，避免一个表单提交覆盖掉另一个表单的设置。
/// </summary>
[ApiEndpoint("PUT", "/api/identity/preferences", typeof(UpdatePreferencesResult), "Profile",
    Summary = "更新个人偏好", Description = "局部更新当前登录用户的外观、通知与显示偏好（null 字段保持不变）", RequireAuthorization = true)]
public record UpdatePreferencesCommand
{
    /// <summary>当前登录用户 ID（由框架从 JWT 自动注入，无需客户端传递）</summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>当前登录用户所属租户（由框架从 JWT 自动注入）</summary>
    public string? CurrentTenantId { get; init; }

    [Description("主题：light / dark / system")]
    public string? Theme { get; init; }

    [Description("界面字体")]
    public string? Font { get; init; }

    [Description("界面语言")]
    public string? Language { get; init; }

    [Description("时区（IANA 名称）")]
    public string? TimeZone { get; init; }

    [Description("推送范围：all / mentions / none")]
    public string? NotifyType { get; init; }

    [Description("接收账户活动邮件")]
    public bool? CommunicationEmails { get; init; }

    [Description("接收产品营销邮件")]
    public bool? MarketingEmails { get; init; }

    [Description("接收社交动态邮件")]
    public bool? SocialEmails { get; init; }

    [Description("移动端使用独立通知设置")]
    public bool? MobileNotifications { get; init; }

    [Description("在侧边栏中被隐藏的菜单项（菜单 url 集合）")]
    public string[]? HiddenSidebarItems { get; init; }
}

public record UpdatePreferencesResult
{
    [Description("是否更新成功")]
    public bool Success { get; init; }
}
