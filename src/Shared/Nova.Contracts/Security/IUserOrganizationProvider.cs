namespace Nova.Contracts.Security;

/// <summary>
/// 共享契约接口：提供跨模块读取用户所属部门（支持单部门与多部门兼任）的标准化能力
/// </summary>
public interface IUserOrganizationProvider
{
    /// <summary>获取用户的主部门 ID</summary>
    Task<Guid?> GetPrimaryOrgIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>获取用户拥有的所有部门 ID 列表（包含兼任部门）</summary>
    Task<List<Guid>> GetUserOrgIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
