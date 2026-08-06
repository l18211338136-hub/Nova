using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/register", typeof(RegisterUserResult), "Auth", Summary = "用户注册")]
public record RegisterUserCommand
{
    [Description("用户名")]
    public string Username { get; init; } = default!;

    [Description("邮箱")]
    public string Email { get; init; } = default!;

    [Description("密码")]
    public string Password { get; init; } = default!;

    [Description("确认密码")]
    public string ConfirmPassword { get; init; } = default!;

    [Description("邮箱验证码")]
    public string EmailCode { get; init; } = default!;
}

public record RegisterUserResult
{
    [Description("用户ID")]
    public Guid UserId { get; init; }
}
