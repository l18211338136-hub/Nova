using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-forgot-password-code", typeof(SendForgotPasswordCodeResult), "Auth", Summary = "发送重置码")]
public record SendForgotPasswordCodeCommand
{
    [Description("邮箱")]
    public string Email { get; init; } = default!;
}

public record SendForgotPasswordCodeResult
{
    [Description("是否成功")]
    public bool Success { get; init; } = true;
}
