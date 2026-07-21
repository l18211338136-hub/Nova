using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.Domain.Entities;

namespace Nova.Framework.MultiTenancy.EntityFrameworkCore;

public static class TenantIsolationExtensions
{
    private const string FinbuckleMultiTenantAnnotation = "Finbuckle:MultiTenant";

    public static void ApplyTenantIsolationByDefault(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (entityType.ClrType is null) continue;
            if (entityType.FindPrimaryKey() is null) continue;
            
            // 跳过那些被显式标记为全局(Global)的实体
            if (typeof(IGlobalEntity).IsAssignableFrom(entityType.ClrType)) continue;
            
            // 跳过那些已经通过其他方式显式标记过多租户配置的实体
            if (entityType.FindAnnotation(FinbuckleMultiTenantAnnotation) is not null) continue;

            // 应用多租户过滤器，并自动拓展唯一索引（追加 TenantId）
            // modelBuilder.Entity(entityType.ClrType).IsMultiTenant().AdjustUniqueIndexes();
            // Finbuckle 7 only provides generic IsMultiTenant<T>()
            var entityMethod = typeof(ModelBuilder).GetMethods()
                .First(m => m.Name == "Entity" && m.IsGenericMethod && m.GetParameters().Length == 0)
                .MakeGenericMethod(entityType.ClrType);
            
            var genericBuilder = entityMethod.Invoke(modelBuilder, null);
            if (genericBuilder == null) continue;
            var genericBuilderType = genericBuilder.GetType();

            var finbuckleAssembly = typeof(IMultiTenantDbContext).Assembly;
            var isMultiTenantMethodInfo = finbuckleAssembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .FirstOrDefault(m => m.Name == "IsMultiTenant" && m.IsGenericMethod);
            
            if (isMultiTenantMethodInfo != null)
            {
                var isMultiTenantMethod = isMultiTenantMethodInfo.MakeGenericMethod(entityType.ClrType);
                genericBuilder = isMultiTenantMethod.Invoke(null, new[] { genericBuilder });
            }

            var adjustUniqueMethodInfo = finbuckleAssembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .FirstOrDefault(m => m.Name == "AdjustUniqueIndexes" && m.IsGenericMethod);
            
            if (adjustUniqueMethodInfo != null && genericBuilder != null)
            {
                var adjustUniqueMethod = adjustUniqueMethodInfo.MakeGenericMethod(entityType.ClrType);
                adjustUniqueMethod.Invoke(null, new[] { genericBuilder });
            }
        }
    }
}
