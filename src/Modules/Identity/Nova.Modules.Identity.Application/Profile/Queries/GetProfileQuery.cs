using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Queries;

[ApiEndpoint("GET", "/api/identity/profile", typeof(ProfileDto), "Profile", Summary = "个人资料", RequireAuthorization = true)]
public record GetProfileQuery
{
    public Guid CurrentUserId { get; init; }
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

    [Description("邮箱已验证")]
    public bool EmailConfirmed { get; init; }

    [Description("手机号")]
    public string? PhoneNumber { get; init; }

    [Description("昵称")]
    public string? NickName { get; init; }

    [Description("头像地址")]
    public string? AvatarUrl { get; init; }

    [Description("简介")]
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

    [Description("显示名称")]
    public string DisplayName { get; init; } = default!;
}
