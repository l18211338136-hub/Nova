using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/register", typeof(RegisterUserResult), "授权认证", Summary = "用户注册", Description = "注册一个新的用户账户")]
public record RegisterUserCommand
{
    [Description("新用户名")]
    public string Username { get; init; } = default!;

    [Description("用户邮箱")]
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
    [Description("新创建用户的唯一标识 ID")]
    public Guid UserId { get; init; }
}
