using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/users/{id}/reset-password", typeof(AdminResetUserPasswordResult), "Users", Summary = "重置用户密码", RequireAuthorization = true)]
[RequirePermission("Identity.Users.ResetPassword")]
public record AdminResetUserPasswordCommand
{
    public Guid Id { get; init; }
    public Guid CurrentUserId { get; init; }
    public string? CurrentTenantId { get; init; }
}

public record AdminResetUserPasswordResult
{
    [Description("是否成功")]
    public bool Success { get; init; }

    [Description("错误信息")]
    public string? Message { get; init; }
}
