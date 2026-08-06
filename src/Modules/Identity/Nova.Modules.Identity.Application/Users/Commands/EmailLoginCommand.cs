using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/email-login", typeof(LoginResult), "Auth", Summary = "验证码登录")]
public record EmailLoginCommand
{
    [Description("邮箱")]
    public string Email { get; init; } = default!;

    [Description("验证码")]
    public string Code { get; init; } = default!;

    [Description("目标租户")]
    public string? TargetTenantId { get; init; }
}
