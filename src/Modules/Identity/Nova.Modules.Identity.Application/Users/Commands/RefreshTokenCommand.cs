using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/refresh", typeof(LoginResult), "Auth", Summary = "刷新令牌")]
public record RefreshTokenCommand
{
    [Description("访问令牌")]
    public string AccessToken { get; init; } = default!;

    [Description("刷新令牌")]
    public string RefreshToken { get; init; } = default!;
}
