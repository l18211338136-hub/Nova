using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Roles.Commands;

[ApiEndpoint("PUT", "/api/identity/roles/{id}", typeof(UpdateRoleResult), "Roles", Summary = "更新角色", Description = "更新一个已存在的角色")]
[RequirePermission("Identity.Roles.Update")]
public record UpdateRoleCommand
{
    [Description("角色 ID")]
    public Guid Id { get; init; }

    [Description("角色名称")]
    public string Name { get; init; } = default!;

    [Description("显示名称")]
    public string DisplayName { get; init; } = default!;

    [Description("角色备注")]
    public string? Remarks { get; init; }

    [Description("排序")]
    public int Sort { get; init; }

    [Description("是否启用")]
    public bool IsEnabled { get; init; }

    [Description("权限标识列表")]
    public List<string>? Permissions { get; init; }

    [Description("菜单标识列表")]
    public List<string>? Menus { get; init; }
}

public record UpdateRoleResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
