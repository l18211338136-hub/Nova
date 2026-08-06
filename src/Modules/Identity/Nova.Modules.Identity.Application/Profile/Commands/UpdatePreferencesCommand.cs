using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Commands;

[ApiEndpoint("PUT", "/api/identity/preferences", typeof(UpdatePreferencesResult), "Profile", Summary = "修改偏好", RequireAuthorization = true)]
public record UpdatePreferencesCommand
{
    public Guid CurrentUserId { get; init; }
    public string? CurrentTenantId { get; init; }

    [Description("主题")]
    public string? Theme { get; init; }

    [Description("字体")]
    public string? Font { get; init; }

    [Description("语言")]
    public string? Language { get; init; }

    [Description("时区")]
    public string? TimeZone { get; init; }

    [Description("通知范围")]
    public string? NotifyType { get; init; }

    [Description("账户邮件")]
    public bool? CommunicationEmails { get; init; }

    [Description("营销邮件")]
    public bool? MarketingEmails { get; init; }

    [Description("社交邮件")]
    public bool? SocialEmails { get; init; }

    [Description("移动通知")]
    public bool? MobileNotifications { get; init; }

    [Description("隐藏菜单")]
    public string[]? HiddenSidebarItems { get; init; }
}

public record UpdatePreferencesResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
