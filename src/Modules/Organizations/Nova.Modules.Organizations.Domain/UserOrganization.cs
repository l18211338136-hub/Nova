using Nova.Framework.Domain.Entities;

namespace Nova.Modules.Organizations.Domain;

/// <summary>
/// 用户 - 组织机构关联实体
/// </summary>
public class UserOrganization : Entity<Guid>
{
    public UserOrganization()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>用户 ID</summary>
    public Guid UserId { get; set; }

    /// <summary>组织机构 ID</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>是否为主机构</summary>
    public bool IsPrimary { get; set; }

    /// <summary>职务 / 岗位名称</summary>
    public string? JobTitle { get; set; }

    /// <summary>关联建立时间</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
