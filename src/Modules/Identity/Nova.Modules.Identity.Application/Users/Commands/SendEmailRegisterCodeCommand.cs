using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-register-code", typeof(SendEmailRegisterCodeResult), "Auth", Summary = "发送注册码")]
public record SendEmailRegisterCodeCommand
{
    [Description("邮箱")]
    public string Email { get; init; } = default!;
}

public record SendEmailRegisterCodeResult
{
    [Description("是否成功")]
    public bool Success { get; init; } = true;
}
