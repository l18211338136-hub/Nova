using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;

using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Menus.Commands;

[ApiEndpoint("PUT", "/api/identity/menus/{id}", typeof(UpdateMenuResult), "Menus", Summary = "更新菜单", Description = "更新系统菜单信息")]
[RequirePermission("Identity.Menus.Update")]
public record UpdateMenuCommand
{
    [FromRoute]
    [Description("菜单ID")]
    public Guid Id { get; init; }

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

public record UpdateMenuResult
{
    [Description("更新后的菜单唯一标识 ID")]
    public Guid MenuId { get; init; }
}
