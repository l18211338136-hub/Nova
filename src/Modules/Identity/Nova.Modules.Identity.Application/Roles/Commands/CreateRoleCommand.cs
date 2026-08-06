using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Roles.Commands;

[ApiEndpoint("POST", "/api/identity/roles", typeof(CreateRoleResult), "Roles", Summary = "创建角色")]
[RequirePermission("Identity.Roles.Create")]
public record CreateRoleCommand
{
    [Description("角色名称")]
    public string Name { get; init; } = default!;

    [Description("显示名称")]
    public string DisplayName { get; init; } = default!;

    [Description("备注")]
    public string? Remarks { get; init; }

    [Description("排序")]
    public int Sort { get; init; }

    [Description("是否启用")]
    public bool IsEnabled { get; init; } = true;

    [Description("权限列表")]
    public List<string>? Permissions { get; init; }
}

public record CreateRoleResult
{
    [Description("角色ID")]
    public Guid RoleId { get; init; }
}
