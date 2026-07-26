using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Roles.Commands;

[ApiEndpoint("POST", "/api/identity/roles", typeof(CreateRoleResult), "Roles", Summary = "创建角色", Description = "创建一个新的角色")]
public record CreateRoleCommand
{
    [Description("角色名称")]
    public string Name { get; init; } = default!;

    [Description("显示名称")]
    public string DisplayName { get; init; } = default!;

    [Description("角色备注")]
    public string? Remarks { get; init; }

    [Description("排序")]
    public int Sort { get; init; }

    [Description("是否启用")]
    public bool IsEnabled { get; init; } = true;

    [Description("权限标识列表")]
    public List<string>? Permissions { get; init; }
}

public record CreateRoleResult
{
    [Description("新创建角色的唯一标识 ID")]
    public Guid RoleId { get; init; }
}
