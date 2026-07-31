using Nova.Contracts.Security;
using System.ComponentModel;

namespace Nova.Modules.Identity.Application.Menus.Queries;

[RequirePermission("Identity.Menus.Read")]
[Description("菜单管理")]
public record MenuDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Name { get; init; } = default!;
    public string Path { get; init; } = default!;
    public string Component { get; init; } = default!;
    public string? Icon { get; init; }
    public int Sort { get; init; }
    public string? Remarks { get; init; }
    public bool IsEnabled { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
