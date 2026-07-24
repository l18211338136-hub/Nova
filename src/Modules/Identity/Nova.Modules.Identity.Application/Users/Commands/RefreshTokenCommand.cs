using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/refresh", typeof(LoginResult), "授权认证", Summary = "刷新令牌", Description = "使用 RefreshToken 获取新的 AccessToken")]
public record RefreshTokenCommand
{
    /// <summary>
    /// 当前的 AccessToken（即使已过期也需要提供，用于提取用户信息）
    /// </summary>
    [Description("当前的 AccessToken（即使已过期也需要提供，用于提取用户信息）")]
    public string AccessToken { get; init; } = default!;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Description("刷新令牌")]
    public string RefreshToken { get; init; } = default!;
}
