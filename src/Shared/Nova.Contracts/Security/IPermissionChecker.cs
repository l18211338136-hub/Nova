using System.Security.Claims;
using Nova.Contracts.DependencyInjection;

namespace Nova.Contracts.Security;

/// <summary>
/// 高性能权限校验服务接口（结合 INovaCache 与层级通配符）
/// </summary>
public interface IPermissionChecker : IScopedDependency
{
    /// <summary>
    /// 校验当前 HttpContext User 是否拥有目标权限
    /// </summary>
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string requiredPermission, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的完整有效权限集合（查缓存或 DB）
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使指定用户的权限缓存失效
    /// </summary>
    Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
