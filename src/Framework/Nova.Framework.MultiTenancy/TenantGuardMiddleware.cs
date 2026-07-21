using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Framework.MultiTenancy;

public class TenantGuardMiddleware
{
    private readonly RequestDelegate _next;

    public TenantGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantContextAccessor = context.RequestServices.GetService<IMultiTenantContextAccessor<NovaTenantInfo>>();
        var tenantInfo = tenantContextAccessor?.MultiTenantContext?.TenantInfo;

        if (tenantInfo != null)
        {
            if (!tenantInfo.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("此租户已被停用，请联系系统管理员。");
                return;
            }

            if (DateTime.UtcNow > tenantInfo.ValidUpto)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("此租户的订阅已过期，请续费后继续使用。");
                return;
            }
        }

        await _next(context);
    }
}
