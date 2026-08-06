using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/login", typeof(LoginResult), "Auth", Summary = "用户登录")]
public record LoginCommand
{
    [Description("账号")]
    public string Account { get; init; } = default!;

    [Description("密码")]
    public string Password { get; init; } = default!;

    [Description("目标租户")]
    public string? TargetTenantId { get; init; }
}

public record TenantOptionDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
}

public record LoginResult
{
    [Description("访问令牌")]
    public string Token { get; init; } = default!;

    [Description("刷新令牌")]
    public string RefreshToken { get; init; } = default!;

    [Description("过期秒数")]
    public int ExpiresIn { get; init; }

    [Description("需选择租户")]
    public bool RequiresTenantSelection { get; init; }

    [Description("可用租户列表")]
    public List<TenantOptionDto>? AvailableTenants { get; init; }
}
