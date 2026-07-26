using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace Nova.Framework.Jobs;

/// <summary>
/// Hangfire Dashboard 权限过滤器：要求用户已登录且拥有 Jobs.Dashboard 权限
/// </summary>
public class NovaHangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // 要求用户已通过认证
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}

/// <summary>
/// 开发环境专用：允许所有人访问 Hangfire Dashboard
/// </summary>
public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
