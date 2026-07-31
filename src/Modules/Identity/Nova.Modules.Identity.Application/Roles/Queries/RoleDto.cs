using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Roles.Queries;

[RequirePermission("Identity.Roles.Read")]
[Description("角色管理")]
public record RoleDto
{
    public Guid Id { get; init; }
    
    [Description("角色名称 (代码)")]
    public string Name { get; init; } = default!;

    [Description("显示名称")]
    public string DisplayName { get; init; } = default!;

    [Description("备注说明")]
    public string? Remarks { get; init; }

    [Description("排序")]
    public int Sort { get; init; }

    [Description("状态")]
    public bool IsEnabled { get; init; }
    
    [Description("创建时间")]
    public DateTimeOffset CreatedAt { get; init; }
}
