using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Queries;

/// <summary>
/// 获取当前登录用户本人的资料。仅需登录态，不校验管理员权限点。
/// </summary>
[ApiEndpoint("GET", "/api/identity/profile", typeof(ProfileDto), "Profile",
    Summary = "获取个人资料", Description = "获取当前登录用户本人的资料信息", RequireAuthorization = true)]
public record GetProfileQuery
{
    /// <summary>当前登录用户 ID（由框架从 JWT 自动注入，无需客户端传递）</summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>当前登录用户所属租户（由框架从 JWT 自动注入）</summary>
    public string? CurrentTenantId { get; init; }
}

public record ProfileDto
{
    [Description("用户ID")]
    public Guid Id { get; init; }

    [Description("用户名")]
    public string UserName { get; init; } = default!;

    [Description("邮箱")]
    public string? Email { get; init; }

    [Description("邮箱是否已验证")]
    public bool EmailConfirmed { get; init; }

    [Description("手机号")]
    public string? PhoneNumber { get; init; }

    [Description("昵称")]
    public string? NickName { get; init; }

    [Description("头像地址")]
    public string? AvatarUrl { get; init; }

    [Description("个人简介")]
    public string? Bio { get; init; }

    [Description("所属角色")]
    public ProfileRoleDto[] Roles { get; init; } = Array.Empty<ProfileRoleDto>();

    [Description("所属租户")]
    public string? TenantId { get; init; }

    [Description("注册时间")]
    public DateTimeOffset CreatedAt { get; init; }
}

public record ProfileRoleDto
{
    [Description("角色名")]
    public string Name { get; init; } = default!;

    [Description("角色显示名称")]
    public string DisplayName { get; init; } = default!;
}
