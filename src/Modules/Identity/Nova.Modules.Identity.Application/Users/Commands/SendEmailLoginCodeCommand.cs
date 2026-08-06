using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.RateLimiting;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/send-login-code", typeof(SendEmailLoginCodeResult), "Auth", Summary = "发送登录码")]
[DistributedRateLimit(5, 60)]
public record SendEmailLoginCodeCommand
{
    [Description("邮箱")]
    public string Email { get; init; } = default!;
}

public record SendEmailLoginCodeResult
{
    [Description("是否成功")]
    public bool Success { get; init; } = true;
}
