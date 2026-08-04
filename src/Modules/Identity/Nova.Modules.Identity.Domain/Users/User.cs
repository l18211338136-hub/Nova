using Microsoft.AspNetCore.Identity;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain.Users;

public class User : IdentityUser<Guid>, IFullAuditedEntity
{
    // 给 EF Core 使用的私有无参构造函数
    private User()
    {
        Id = Guid.CreateVersion7();
    }

    // 强约束的私有构造函数
    private User(string userName, string email, string? remarks = null, int sort = 0) : this()
    {
        UserName = userName;
        Email = email;
        EmailConfirmed = true;
        IsEnabled = true;
        Remarks = remarks;
        Sort = sort;
    }

    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Remarks { get; private set; }
    public int Sort { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    /// <summary>昵称／对外显示名。为空时前端回退显示 UserName。</summary>
    public string? NickName { get; private set; }

    /// <summary>头像地址（URL 或存储对象键）。</summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>个人简介。</summary>
    public string? Bio { get; private set; }

    // Factory method
    public static User Create(string userName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));

        var user = new User(userName, email);
        // 启用账号锁定，使登录失败计数 / 防暴力破解生效
        user.LockoutEnabled = true;
        return user;
    }

    /// <summary>
    /// 更新本人可自助维护的资料字段。空白字符串一律规范化为 null，避免库里出现 "" 与 null 两种空值。
    /// </summary>
    public void UpdateProfile(string? nickName, string? bio, string? avatarUrl)
    {
        NickName = Normalize(nickName);
        Bio = Normalize(bio);
        AvatarUrl = Normalize(avatarUrl);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}
