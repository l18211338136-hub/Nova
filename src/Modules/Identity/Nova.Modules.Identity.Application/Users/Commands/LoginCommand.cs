using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/login", typeof(LoginResult), "Auth", Summary = "用户登录", Description = "通过用户名和密码获取身份认证 Token")]
public record LoginCommand
{
    /// <summary>
    /// 账号 (用户名或邮箱)
    /// </summary>
    [Description("账号 (用户名或邮箱)")]
    public string Account { get; init; } = default!;

    /// <summary>
    /// 密码
    /// </summary>
    [Description("密码")]
    public string Password { get; init; } = default!;

    [Description("当拥有多个企业时，需指定要登录的目标租户ID")]
    public string? TargetTenantId { get; init; }
}

public record TenantOptionDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
}

public record LoginResult
{
    /// <summary>
    /// JWT Token，需在请求头带上 Bearer {Token}
    /// </summary>
    [Description("JWT Token，需在请求头带上 Bearer {Token}")]
    public string Token { get; init; } = default!;

    /// <summary>
    /// 刷新令牌，用于在 AccessToken 过期后获取新的 Token
    /// </summary>
    [Description("刷新令牌，用于在 AccessToken 过期后获取新的 Token")]
    public string RefreshToken { get; init; } = default!;

    /// <summary>
    /// 访问令牌的过期时间（单位：秒）
    /// </summary>
    [Description("访问令牌的过期时间（单位：秒）")]
    public int ExpiresIn { get; init; }

    [Description("是否需要选择租户（当密码能在多个租户匹配成功时返回 true）")]
    public bool RequiresTenantSelection { get; init; }

    [Description("可选的租户列表")]
    public List<TenantOptionDto>? AvailableTenants { get; init; }
}
