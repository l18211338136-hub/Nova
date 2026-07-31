using System.ComponentModel;
using Nova.Contracts.CQRS;

using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Menus.Commands;

[ApiEndpoint("POST", "/api/identity/menus", typeof(CreateMenuResult), "Menus", Summary = "创建菜单", Description = "创建一个新的系统菜单")]
[RequirePermission("Identity.Menus.Create")]
public record CreateMenuCommand
{
    [Description("父级ID")]
    public Guid? ParentId { get; init; }

    [Description("菜单名称")]
    public string Name { get; init; } = default!;

    [Description("路由地址")]
    public string Path { get; init; } = default!;

    [Description("组件路径")]
    public string Component { get; init; } = default!;

    [Description("图标名称")]
    public string? Icon { get; init; }

    [Description("备注说明")]
    public string? Remarks { get; init; }

    [Description("排序")]
    public int Sort { get; init; }

    [Description("是否启用")]
    public bool IsEnabled { get; init; } = true;
}

public record CreateMenuResult
{
    [Description("新创建菜单的唯一标识 ID")]
    public Guid MenuId { get; init; }
}
