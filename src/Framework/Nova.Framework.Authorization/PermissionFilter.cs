using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Security;

namespace Nova.Framework.Authorization;

/// <summary>
/// 细粒度权限点与层级通配符端点拦截器
/// </summary>
public class PermissionFilter : IEndpointFilter
{
    private readonly string _permission;

    public PermissionFilter(string permission)
    {
        _permission = permission;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;
        
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var permissionChecker = context.HttpContext.RequestServices?.GetService<IPermissionChecker>();
        bool hasPermission;

        if (permissionChecker != null)
        {
            hasPermission = await permissionChecker.HasPermissionAsync(user, _permission, context.HttpContext.RequestAborted);
        }
        else
        {
            // 降级回退：使用 Claims + PermissionMatcher 通配符比对
            var claims = user.Claims.Where(c => c.Type == "Permission").Select(c => c.Value);
            hasPermission = PermissionMatcher.IsMatchAny(claims, _permission);
        }
        
        if (!hasPermission)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
