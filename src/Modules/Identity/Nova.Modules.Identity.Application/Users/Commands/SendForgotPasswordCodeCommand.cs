using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-forgot-password-code", typeof(SendForgotPasswordCodeResult), "Identity", Summary = "发送忘记密码验证码", Description = "通过邮箱发送用于重置密码的 6 位数验证码")]
public record SendForgotPasswordCodeCommand
{
    [Description("注册邮箱")]
    public string Email { get; init; } = default!;
}

public record SendForgotPasswordCodeResult
{
    [Description("发送结果")]
    public bool Success { get; init; } = true;
}
