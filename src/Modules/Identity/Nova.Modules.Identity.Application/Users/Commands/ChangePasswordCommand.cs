using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Idempotency;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/change-password", typeof(ChangePasswordResult), "Auth", Summary = "修改密码", RequireAuthorization = true)]
[Idempotent(5)]
public record ChangePasswordCommand
{
    public Guid CurrentUserId { get; init; }
    public string? CurrentTenantId { get; init; }

    [Description("旧密码")]
    public string OldPassword { get; init; } = default!;

    [Description("新密码")]
    public string NewPassword { get; init; } = default!;
}

public record ChangePasswordResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
