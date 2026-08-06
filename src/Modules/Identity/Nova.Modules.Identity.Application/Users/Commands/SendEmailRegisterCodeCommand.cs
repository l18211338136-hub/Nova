using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Idempotency;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-register-code", typeof(SendEmailRegisterCodeResult), "Auth", Summary = "发送注册码")]
[Idempotent(5)]
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
