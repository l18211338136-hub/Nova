using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Menus.Commands;

[ApiEndpoint("DELETE", "/api/identity/menus/{id}", typeof(DeleteMenuResult), "Menus", Summary = "删除菜单")]
[RequirePermission("Identity.Menus.Delete")]
public record DeleteMenuCommand
{
    [FromRoute]
    [Description("菜单ID")]
    public Guid Id { get; init; }
}

public record DeleteMenuResult
{
    public bool Success { get; init; }
}
