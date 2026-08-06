using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("PUT", "/api/identity/users/{id}", typeof(UpdateUserResult), "Users", Summary = "更新用户")]
[RequirePermission("Identity.Users.Update")]
public record UpdateUserCommand
{
    public Guid Id { get; init; }

    [Description("用户名")]
    public string UserName { get; init; } = default!;

    [Description("邮箱")]
    public string Email { get; init; } = default!;

    [Description("密码")]
    public string? Password { get; init; }

    [Description("手机号")]
    public string? PhoneNumber { get; init; }

    [Description("是否启用")]
    public bool IsEnabled { get; init; } = true;

    [Description("角色列表")]
    public List<string>? Roles { get; init; }

    [Description("权限列表")]
    public List<string>? Permissions { get; init; }

    [Description("菜单列表")]
    public List<string>? Menus { get; init; }
}

public record UpdateUserResult
{
    public bool Success { get; init; }
}
