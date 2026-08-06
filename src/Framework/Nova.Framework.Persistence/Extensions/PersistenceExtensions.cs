using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Persistence.Interceptors;

namespace Nova.Framework.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static DbContextOptionsBuilder AddNovaInterceptors(
        this DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
    {
        // 1. 自动挂载审计时间/修改人拦截器
        var auditableInterceptor = serviceProvider.GetService<AuditableEntitySaveChangesInterceptor>();
        if (auditableInterceptor != null)
        {
            options.AddInterceptors(auditableInterceptor);
        }

        // 2. 自动挂载数据行级变更 Diff 追溯拦截器
        var changeInterceptor = serviceProvider.GetService<EntityChangeCaptureInterceptor>();
        if (changeInterceptor != null)
        {
            options.AddInterceptors(changeInterceptor);
        }

        // 3. 自动挂载 UTC 时间转换拦截器
        var utcInterceptor = serviceProvider.GetService<UtcDateTimeParameterInterceptor>();
        if (utcInterceptor != null)
        {
            options.AddInterceptors(utcInterceptor);
        }

        return options;
    }
}
