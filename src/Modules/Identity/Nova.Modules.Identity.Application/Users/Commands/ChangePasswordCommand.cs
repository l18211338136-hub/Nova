using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/change-password", typeof(ChangePasswordResult), "Auth", Summary = "修改密码", Description = "登录态下凭旧密码修改新密码")]
[RequirePermission("Identity.Users.ChangePassword")]
public record ChangePasswordCommand
{
    /// <summary>
    /// 当前登录用户 ID（由框架从 JWT 自动注入，无需客户端传递）
    /// </summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>
    /// 当前登录用户所属租户（由框架从 JWT 自动注入）
    /// </summary>
    public string? CurrentTenantId { get; init; }

    [Description("旧密码")]
    public string OldPassword { get; init; } = default!;

    [Description("新密码")]
    public string NewPassword { get; init; } = default!;
}

public record ChangePasswordResult
{
    [Description("是否修改成功")]
    public bool Success { get; init; }
}
