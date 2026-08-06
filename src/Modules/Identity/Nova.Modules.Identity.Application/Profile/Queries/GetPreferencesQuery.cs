using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Queries;

[ApiEndpoint("GET", "/api/identity/preferences", typeof(UserPreferenceDto), "Profile", Summary = "个人偏好", RequireAuthorization = true)]
public record GetPreferencesQuery
{
    public Guid CurrentUserId { get; init; }
    public string? CurrentTenantId { get; init; }
}

public record UserPreferenceDto
{
    [Description("主题")]
    public string Theme { get; init; } = "system";

    [Description("字体")]
    public string Font { get; init; } = "inter";

    [Description("语言")]
    public string Language { get; init; } = "zh-CN";

    [Description("时区")]
    public string? TimeZone { get; init; }

    [Description("通知范围")]
    public string NotifyType { get; init; } = "all";

    [Description("账户邮件")]
    public bool CommunicationEmails { get; init; }

    [Description("营销邮件")]
    public bool MarketingEmails { get; init; }

    [Description("社交邮件")]
    public bool SocialEmails { get; init; }

    [Description("安全邮件")]
    public bool SecurityEmails { get; init; } = true;

    [Description("移动通知")]
    public bool MobileNotifications { get; init; }

    [Description("隐藏菜单")]
    public string[] HiddenSidebarItems { get; init; } = Array.Empty<string>();
}
