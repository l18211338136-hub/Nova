using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/reset-password", typeof(ResetPasswordResult), "Auth", Summary = "重置密码", Description = "通过邮箱验证码重置用户密码")]
public record ResetPasswordCommand
{
    [Description("注册邮箱")]
    public string Email { get; init; } = default!;

    [Description("验证码")]
    public string Code { get; init; } = default!;

    [Description("新密码")]
    public string NewPassword { get; init; } = default!;
}

public record ResetPasswordResult
{
    [Description("是否成功")]
    public bool Success { get; init; }

    [Description("错误信息")]
    public string? Message { get; init; }
}
