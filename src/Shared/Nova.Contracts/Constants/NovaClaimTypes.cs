namespace Nova.Contracts.Constants;

/// <summary>
/// 全局 Claim 类型常量定义（在 Nova 全局共享契约层统一维护）
/// </summary>
public static class NovaClaimTypes
{
    /// <summary>部门/组织机构 ID</summary>
    public const string OrgId = "OrgId";

    /// <summary>组织机构 ID 别名</summary>
    public const string OrganizationId = "OrganizationId";

    /// <summary>租户 ID</summary>
    public const string TenantId = "tenantId";

    /// <summary>操作权限 Claim</summary>
    public const string Permission = "Permission";

    /// <summary>菜单权限 Claim</summary>
    public const string Menu = "Menu";
}
