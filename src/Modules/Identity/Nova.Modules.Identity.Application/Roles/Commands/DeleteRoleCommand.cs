using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Roles.Commands;

[ApiEndpoint("DELETE", "/api/identity/roles/{id}", typeof(DeleteRoleResult), "Roles", Summary = "删除角色", Description = "删除一个已存在的角色")]
[RequirePermission("Identity.Roles.Delete")]
public record DeleteRoleCommand
{
    [Description("角色 ID")]
    public Guid Id { get; init; }
}

public record DeleteRoleResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
