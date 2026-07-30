using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/email-login", typeof(LoginResult), "Auth", Summary = "邮箱验证码登录", Description = "通过邮箱和收到的验证码直接登录获取 Token")]
public record EmailLoginCommand
{
    [Description("注册邮箱")]
    public string Email { get; init; } = default!;

    [Description("6 位数验证码")]
    public string Code { get; init; } = default!;

    [Description("当拥有多个企业时，需指定要登录的目标租户ID")]
    public string? TargetTenantId { get; init; }
}
