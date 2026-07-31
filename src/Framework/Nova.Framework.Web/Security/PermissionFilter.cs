using Microsoft.AspNetCore.Http;

namespace Nova.Framework.Web.Security;

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

        // 检查是否有精准权限，或者拥有 '*' 绝对通配符
        var hasPermission = user.HasClaim(c => c.Type == "Permission" && (c.Value == _permission || c.Value == "*"));
        
        if (!hasPermission)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
