using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/logout", typeof(LogoutResult), "Auth", Summary = "退出登录")]
public record LogoutCommand
{
    [Description("访问令牌")]
    public string AccessToken { get; init; } = default!;

    [Description("刷新令牌")]
    public string RefreshToken { get; init; } = default!;
}

public record LogoutResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
