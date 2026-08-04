using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Queries;

/// <summary>
/// 获取当前登录用户的个性化偏好（外观 / 通知 / 显示）。首次访问时返回默认值。
/// </summary>
[ApiEndpoint("GET", "/api/identity/preferences", typeof(UserPreferenceDto), "Profile",
    Summary = "获取个人偏好", Description = "获取当前登录用户的外观、通知与显示偏好设置", RequireAuthorization = true)]
public record GetPreferencesQuery
{
    /// <summary>当前登录用户 ID（由框架从 JWT 自动注入，无需客户端传递）</summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>当前登录用户所属租户（由框架从 JWT 自动注入）</summary>
    public string? CurrentTenantId { get; init; }
}

public record UserPreferenceDto
{
    [Description("主题：light / dark / system")]
    public string Theme { get; init; } = "system";

    [Description("界面字体")]
    public string Font { get; init; } = "inter";

    [Description("界面语言")]
    public string Language { get; init; } = "zh-CN";

    [Description("时区（IANA 名称）")]
    public string? TimeZone { get; init; }

    [Description("推送范围：all / mentions / none")]
    public string NotifyType { get; init; } = "all";

    [Description("接收账户活动邮件")]
    public bool CommunicationEmails { get; init; }

    [Description("接收产品营销邮件")]
    public bool MarketingEmails { get; init; }

    [Description("接收社交动态邮件")]
    public bool SocialEmails { get; init; }

    [Description("接收安全提醒邮件（强制开启）")]
    public bool SecurityEmails { get; init; } = true;

    [Description("移动端使用独立通知设置")]
    public bool MobileNotifications { get; init; }

    [Description("在侧边栏中被隐藏的菜单项（菜单 url 集合）")]
    public string[] HiddenSidebarItems { get; init; } = Array.Empty<string>();
}
