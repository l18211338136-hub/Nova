namespace Nova.Contracts.Security;

/// <summary>
/// 权限通配符匹配算法帮助类（支持精准匹配、全局 * 通配符与 Identity.Users.* / Identity.* 等层级通配符）
/// </summary>
public static class PermissionMatcher
{
    /// <summary>
    /// 判断单一用户权限声明是否能够匹配目标请求权限
    /// </summary>
    public static bool IsMatch(string? userPermission, string? requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(userPermission) || string.IsNullOrWhiteSpace(requiredPermission))
            return false;

        userPermission = userPermission.Trim();
        requiredPermission = requiredPermission.Trim();

        // 1. 精确匹配或超级全局通配符 *
        if (userPermission == "*" || string.Equals(userPermission, requiredPermission, StringComparison.OrdinalIgnoreCase))
            return true;

        // 2. 层级/前缀通配符匹配（例如 Identity.Users.* 或 Identity.*）
        if (userPermission.EndsWith(".*", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = userPermission.Substring(0, userPermission.Length - 1); // 保留结尾点号 "Identity.Users."
            if (requiredPermission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断用户包含的所有权限集合中，是否有任意一条匹配目标请求权限
    /// </summary>
    public static bool IsMatchAny(IEnumerable<string>? userPermissions, string? requiredPermission)
    {
        if (userPermissions == null || string.IsNullOrWhiteSpace(requiredPermission))
            return false;

        foreach (var userPerm in userPermissions)
        {
            if (IsMatch(userPerm, requiredPermission))
                return true;
        }

        return false;
    }
}
