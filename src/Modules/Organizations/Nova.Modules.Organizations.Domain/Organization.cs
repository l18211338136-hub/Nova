using Nova.Contracts.Security;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Organizations.Domain;

/// <summary>
/// 组织机构领域实体
/// </summary>
[AbacEntity("组织机构表")]
public class Organization : FullAuditedEntity<Guid>
{
    public Organization()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>父机构 ID</summary>
    public Guid? ParentId { get; set; }

    /// <summary>机构名称</summary>
    [AbacField("机构名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>机构编码</summary>
    [AbacField("机构编码")]
    public string? Code { get; set; }

    /// <summary>机构类型字典编码 (对接数据字典 sys_org_type，如 group, company, department, team)</summary>
    [AbacField("机构类型")]
    public string? Type { get; set; }

    /// <summary>机构层级/等级（1级为根节点，子节点为 Parent.Level + 1）</summary>
    [AbacField("机构层级")]
    public int Level { get; set; } = 1;

    /// <summary>机构负责人用户 ID</summary>
    public Guid? LeaderUserId { get; set; }

    /// <summary>联系电话</summary>
    [AbacField("联系电话", supportMasking: true)]
    public string? Phone { get; set; }

    /// <summary>联系邮箱</summary>
    [AbacField("联系邮箱", supportMasking: true)]
    public string? Email { get; set; }

    /// <summary>行级数据权限范围 (1:全部数据, 2:本部门数据, 3:本部门及下级部门数据, 4:仅本人数据)</summary>
    public int DataScope { get; set; } = 2;

    /// <summary>按业务实体/数据表划分的多资源 ABAC 动态策略 JSON</summary>
    public string? AbacPoliciesJson { get; set; }

    public static Organization Create(
        string name,
        Guid? parentId = null,
        string? code = null,
        string? type = null,
        int level = 1,
        int sort = 0,
        Guid? leaderUserId = null,
        string? phone = null,
        string? email = null,
        string? remarks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new Organization
        {
            Name = name.Trim(),
            ParentId = parentId,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim(),
            Level = Math.Max(1, level),
            Sort = sort,
            LeaderUserId = leaderUserId,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
            IsEnabled = true
        };
    }

    public void Update(
        string name,
        Guid? parentId,
        string? code,
        string? type,
        int level,
        int sort,
        Guid? leaderUserId,
        string? phone,
        string? email,
        string? remarks,
        bool isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        ParentId = parentId;
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        Level = Math.Max(1, level);
        Sort = sort;
        LeaderUserId = leaderUserId;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        IsEnabled = isEnabled;
    }
}
