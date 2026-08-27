namespace Nova.Modules.Organizations.Application.DTOs;

public class OrganizationPermissionsDto
{
    public Guid OrganizationId { get; set; }
    public int DataScope { get; set; } = 2; // 2 = 本部门数据
    public string? AbacPoliciesJson { get; set; }
}
