using System.Collections.Generic;

namespace Nova.Modules.Identity.Application.Roles.Queries;

public record RolePermissionsDto
{
    public List<string> Permissions { get; init; } = new();
    public List<string> Menus { get; init; } = new();
}
