using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Caching;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Security;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Infrastructure.Security;

/// <summary>
/// 高性能安全防御型权限校验器实现类。
/// 支持 INovaCache (FusionCache L1 本地内存 + L2 Redis) 双层缓存与通用层级通配符校验。
/// 包含全动态 RoleClaims 读取、Root 常量判断与越权 `*` 过滤防护。
/// </summary>
public class PermissionChecker : IPermissionChecker, IScopedDependency
{
    private readonly INovaCache _cache;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PermissionChecker> _logger;

    public PermissionChecker(
        INovaCache cache,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration configuration,
        ILogger<PermissionChecker> logger)
    {
        _cache = cache;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string requiredPermission, CancellationToken cancellationToken = default)
    {
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return false;
        }

        var useCached = _configuration.GetValue<bool>("Auth:UseCachedPermissions", true);

        if (!useCached)
        {
            // 降级模式：从 JWT Claims 中比对
            var jwtPermissions = user.Claims.Where(c => c.Type == "Permission").Select(c => c.Value);
            return PermissionMatcher.IsMatchAny(jwtPermissions, requiredPermission);
        }

        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(nameIdentifier, out var userId))
        {
            return false;
        }

        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return PermissionMatcher.IsMatchAny(userPermissions, requiredPermission);
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"Auth:Permissions:{userId}";

        var permissions = await _cache.GetOrSetAsync<HashSet<string>>(
            cacheKey,
            async token =>
            {
                var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return result;
                }

                // 获取用户所属的所有角色名称列表
                var roles = await _userManager.GetRolesAsync(user);
                var isRootUser = roles.Contains(NovaIdentityConstants.Roles.Root, StringComparer.OrdinalIgnoreCase);

                // 1. 只有 Root 超级管理员才能获得全局通配符 *
                if (isRootUser)
                {
                    result.Add("*");
                }

                // 2. 获取用户独立 Claims（非 Root 过滤掉可能误录入的裸 * 越权 Claim）
                var userClaims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in userClaims.Where(c => c.Type == "Permission"))
                {
                    if (isRootUser || claim.Value != "*")
                    {
                        result.Add(claim.Value);
                    }
                }

                // 3. 全动态获取用户所属角色的 Claims (包含 Admin 及所有自定义角色)
                foreach (var roleName in roles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var roleClaims = await _roleManager.GetClaimsAsync(role);
                        foreach (var claim in roleClaims.Where(c => c.Type == "Permission"))
                        {
                            if (isRootUser || claim.Value != "*")
                            {
                                result.Add(claim.Value);
                            }
                        }
                    }
                }

                _logger.LogDebug("[PermissionChecker] Loaded {Count} permissions for user {UserId} from DB", result.Count, userId);
                return result;
            },
            TimeSpan.FromMinutes(30),
            cancellationToken);

        return permissions ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"Auth:Permissions:{userId}";
        await _cache.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogInformation("[PermissionChecker] Evicted permission cache for user {UserId}", userId);
    }
}
