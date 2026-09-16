using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Security;

namespace Nova.Framework.Authorization;

/// <summary>
/// MVC Controller 版的 [RequirePermission] 执行器（与 PermissionFilter 语义一致）。
/// 声明式端点（record + [ApiEndpoint]）由 AutoEndpointExtensions 挂 PermissionFilter（IEndpointFilter）拦截；
/// 模块中的 MVC Controller（如 MCP 模块）没有该 EndpointFilter 管线，
/// 由本过滤器在 AddModules 中全局挂载，使 [RequirePermission] 对 Controller 同样生效。
/// 类级与方法级同时声明时需全部满足（AllowMultiple = true 的 AND 语义）。
/// </summary>
public class RequirePermissionActionFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return;
        }

        var permissions = descriptor.MethodInfo
            .GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
            .Concat(descriptor.ControllerTypeInfo.GetCustomAttributes<RequirePermissionAttribute>(inherit: true))
            .Select(a => a.Permission)
            .Distinct()
            .ToArray();

        // 未声明 [RequirePermission] 的 Controller Action 不拦截（鉴权仍由 [Authorize] 负责）
        if (permissions.Length == 0)
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionChecker = context.HttpContext.RequestServices.GetService<IPermissionChecker>();
        foreach (var permission in permissions)
        {
            bool hasPermission;
            if (permissionChecker is not null)
            {
                hasPermission = await permissionChecker.HasPermissionAsync(user, permission, context.HttpContext.RequestAborted);
            }
            else
            {
                // 降级回退：使用 Claims + PermissionMatcher 通配符比对（与 PermissionFilter 一致）
                var claims = user.Claims.Where(c => c.Type == "Permission").Select(c => c.Value);
                hasPermission = PermissionMatcher.IsMatchAny(claims, permission);
            }

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
