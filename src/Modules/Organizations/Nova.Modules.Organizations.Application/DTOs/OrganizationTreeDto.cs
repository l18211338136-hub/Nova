namespace Nova.Modules.Organizations.Application.DTOs;

public class OrganizationTreeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Type { get; set; }
    public int Level { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public int MemberCount { get; set; }
    public List<OrganizationTreeDto> Children { get; set; } = new();
}
