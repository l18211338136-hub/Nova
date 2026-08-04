using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Commands;

/// <summary>
/// 更新当前登录用户本人的资料。仅需登录态。
/// 注意：用户名与邮箱涉及登录凭据与全局映射表，不在此端点开放修改。
/// </summary>
[ApiEndpoint("PUT", "/api/identity/profile", typeof(UpdateProfileResult), "Profile",
    Summary = "更新个人资料", Description = "更新当前登录用户的昵称、简介、头像与手机号", RequireAuthorization = true)]
public record UpdateProfileCommand
{
    /// <summary>当前登录用户 ID（由框架从 JWT 自动注入，无需客户端传递）</summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>当前登录用户所属租户（由框架从 JWT 自动注入）</summary>
    public string? CurrentTenantId { get; init; }

    [Description("昵称")]
    public string? NickName { get; init; }

    [Description("个人简介")]
    public string? Bio { get; init; }

    [Description("头像地址")]
    public string? AvatarUrl { get; init; }

    [Description("手机号")]
    public string? PhoneNumber { get; init; }
}

public record UpdateProfileResult
{
    [Description("是否更新成功")]
    public bool Success { get; init; }
}
