using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-login-code", typeof(SendEmailLoginCodeResult), "Auth", Summary = "发送登录验证码", Description = "通过邮箱发送用于免密登录的 6 位数验证码")]
public record SendEmailLoginCodeCommand
{
    [Description("注册邮箱")]
    public string Email { get; init; } = default!;
}

public record SendEmailLoginCodeResult
{
    [Description("发送结果")]
    public bool Success { get; init; } = true;
}
