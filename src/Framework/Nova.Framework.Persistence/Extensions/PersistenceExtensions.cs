using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Persistence.Interceptors;
using Nova.Framework.Persistence.Outbox;

namespace Nova.Framework.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddNovaOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OutboxDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetService<Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor>()?.MultiTenantContext?.TenantInfo;
            
            // 使用反射获取 ConnectionString，以避免底层 Persistence 层去反向依赖 MultiTenancy 层产生循环引用
            var tenantConnString = tenantInfo?.GetType().GetProperty("ConnectionString")?.GetValue(tenantInfo) as string;
            var connectionString = tenantConnString ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.AddNovaInterceptors(sp);
        });
        return services;
    }
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
