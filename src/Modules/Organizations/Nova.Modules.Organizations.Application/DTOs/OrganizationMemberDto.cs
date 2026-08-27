namespace Nova.Modules.Organizations.Application.DTOs;

public class OrganizationMemberDto
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
    public string? JobTitle { get; set; }
    public bool IsLeader { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
