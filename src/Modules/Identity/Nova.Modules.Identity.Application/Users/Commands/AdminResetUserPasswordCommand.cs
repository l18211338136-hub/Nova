using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/users/{id}/reset-password", typeof(AdminResetUserPasswordResult), "Users", Summary = "重置用户密码", Description = "需要权限；向目标用户邮箱发送密码重置验证码", RequireAuthorization = true)]
[RequirePermission("Identity.Users.ResetPassword")]
public record AdminResetUserPasswordCommand
{
    /// <summary>
    /// 目标用户 ID（来自路由 {id}）
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 当前登录用户 ID（由框架从 JWT 自动注入，无需客户端传递）
    /// </summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>
    /// 当前登录用户所属租户（由框架从 JWT 自动注入）
    /// </summary>
    public string? CurrentTenantId { get; init; }
}

public record AdminResetUserPasswordResult
{
    [Description("是否成功")]
    public bool Success { get; init; }

    [Description("错误信息")]
    public string? Message { get; init; }
}
