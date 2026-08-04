using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain.Users;

/// <summary>
/// 用户个性化偏好。一个用户一行，覆盖设置页的「外观 / 通知 / 显示」三块。
/// 采用结构化列而非 JSON 大字段：便于 OpenAPI 生成强类型、便于后续按列查询与统计。
/// 多租户隔离方式与 User 一致（Finbuckle 阴影属性 TenantId）。
/// </summary>
public class UserPreference : IFullAuditedEntity
{
    private UserPreference()
    {
    }

    private UserPreference(Guid userId) : this()
    {
        UserId = userId;
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>所属用户。一个用户至多一行。</summary>
    public Guid UserId { get; private set; }

    // ── 外观 ──────────────────────────────────────────────
    /// <summary>主题：light / dark / system</summary>
    public string Theme { get; private set; } = "system";

    /// <summary>界面字体，取值需在前端 fonts 配置内。</summary>
    public string Font { get; private set; } = "inter";

    // ── 账户 ──────────────────────────────────────────────
    /// <summary>界面语言，如 zh-CN / en-US。</summary>
    public string Language { get; private set; } = "zh-CN";

    /// <summary>IANA 时区名，如 Asia/Shanghai。</summary>
    public string? TimeZone { get; private set; }

    // ── 通知 ──────────────────────────────────────────────
    /// <summary>推送范围：all / mentions / none</summary>
    public string NotifyType { get; private set; } = "all";

    public bool CommunicationEmails { get; private set; }
    public bool MarketingEmails { get; private set; }
    public bool SocialEmails { get; private set; } = true;

    /// <summary>安全类邮件。出于账号安全考虑始终开启，前端置灰。</summary>
    public bool SecurityEmails { get; private set; } = true;

    /// <summary>移动端是否使用独立的通知设置。</summary>
    public bool MobileNotifications { get; private set; }

    // ── 显示 ──────────────────────────────────────────────
    /// <summary>
    /// 在侧边栏中被用户隐藏的菜单项，以英文逗号分隔的菜单 url 集合。
    /// 存「隐藏项」而非「显示项」：新增菜单时默认可见，无需回填历史数据。
    /// </summary>
    public string? HiddenSidebarItems { get; private set; }

    // ── IFullAuditedEntity ────────────────────────────────
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Remarks { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>为指定用户创建一份默认偏好。</summary>
    public static UserPreference CreateDefault(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("用户标识不能为空", nameof(userId));
        }

        return new UserPreference(userId);
    }

    public void UpdateAppearance(string? theme, string? font)
    {
        if (!string.IsNullOrWhiteSpace(theme)) Theme = theme.Trim();
        if (!string.IsNullOrWhiteSpace(font)) Font = font.Trim();
    }

    public void UpdateLocale(string? language, string? timeZone)
    {
        if (!string.IsNullOrWhiteSpace(language)) Language = language.Trim();
        TimeZone = string.IsNullOrWhiteSpace(timeZone) ? null : timeZone.Trim();
    }

    public void UpdateNotifications(
        string? notifyType,
        bool communicationEmails,
        bool marketingEmails,
        bool socialEmails,
        bool mobileNotifications)
    {
        if (!string.IsNullOrWhiteSpace(notifyType)) NotifyType = notifyType.Trim();
        CommunicationEmails = communicationEmails;
        MarketingEmails = marketingEmails;
        SocialEmails = socialEmails;
        MobileNotifications = mobileNotifications;
        // SecurityEmails 不开放修改：安全类通知强制送达。
    }

    public void UpdateHiddenSidebarItems(IEnumerable<string>? hiddenItems)
    {
        var items = (hiddenItems ?? Enumerable.Empty<string>())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        HiddenSidebarItems = items.Length == 0 ? null : string.Join(',', items);
    }

    /// <summary>把逗号分隔的隐藏项还原为集合。</summary>
    public IReadOnlyList<string> GetHiddenSidebarItems()
        => string.IsNullOrWhiteSpace(HiddenSidebarItems)
            ? Array.Empty<string>()
            : HiddenSidebarItems.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
