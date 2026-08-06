using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("POST", "/api/identity/users", typeof(CreateUserResult), "Users", Summary = "创建用户")]
[RequirePermission("Identity.Users.Create")]
public record CreateUserCommand
{
    [Description("用户名")]
    public string UserName { get; init; } = default!;

    [Description("邮箱")]
    public string Email { get; init; } = default!;

    [Description("密码")]
    public string Password { get; init; } = default!;

    [Description("手机号")]
    public string? PhoneNumber { get; init; }

    [Description("是否启用")]
    public bool IsEnabled { get; init; } = true;

    [Description("角色列表")]
    public List<string>? Roles { get; init; }

    [Description("权限列表")]
    public List<string>? Permissions { get; init; }
}

public record CreateUserResult
{
    [Description("用户ID")]
    public Guid UserId { get; init; }
}
