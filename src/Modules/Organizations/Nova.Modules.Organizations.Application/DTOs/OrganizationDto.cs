using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Organizations.Application.DTOs;

[RequirePermission("Organization.Orgs.Read")]
[Description("组织架构")]
public class OrganizationDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Type { get; set; }
    public int Level { get; set; }
    public int Sort { get; set; }
    public Guid? LeaderUserId { get; set; }
    public string? LeaderUserName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsEnabled { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int MemberCount { get; set; }
}
