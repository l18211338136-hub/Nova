using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-register-code", typeof(SendEmailRegisterCodeResult), "Auth", Summary = "发送注册验证码", Description = "通过邮箱发送用于注册的 6 位数验证码")]
public record SendEmailRegisterCodeCommand
{
    [Description("注册邮箱")]
    public string Email { get; init; } = default!;
}

public record SendEmailRegisterCodeResult
{
    [Description("发送结果")]
    public bool Success { get; init; } = true;
}
