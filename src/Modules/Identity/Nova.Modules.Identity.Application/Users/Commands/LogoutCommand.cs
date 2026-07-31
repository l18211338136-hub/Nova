using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/logout", typeof(LogoutResult), "Auth", Summary = "登出（吊销刷新令牌）", Description = "吊销当前刷新令牌，使其无法再用于获取新的 AccessToken")]
public record LogoutCommand
{
    /// <summary>
    /// 当前的 AccessToken（即使已过期也需提供，用于提取用户与租户信息）
    /// </summary>
    [Description("当前的 AccessToken（即使已过期也需提供，用于提取用户与租户信息）")]
    public string AccessToken { get; init; } = default!;

    /// <summary>
    /// 要吊销的刷新令牌
    /// </summary>
    [Description("要吊销的刷新令牌")]
    public string RefreshToken { get; init; } = default!;
}

public record LogoutResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
