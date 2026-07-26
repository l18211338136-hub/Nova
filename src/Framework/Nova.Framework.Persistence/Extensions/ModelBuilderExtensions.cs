using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.Domain.Auditing;

namespace Nova.Framework.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IFullAuditedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IFullAuditedEntity.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var softDeleteLambda = Expression.Lambda(condition, parameter);

                // 通过反射调用 EF Core 10 的命名查询过滤器 API (SetQueryFilter(string, LambdaExpression))
                var namedFilterMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType)
                    .GetMethod("SetQueryFilter", new[] { typeof(string), typeof(LambdaExpression) });

                if (namedFilterMethod != null)
                {
                    // EF Core 10+ 逻辑：分配独立的 Named Filter，避免和多租户冲突
                    namedFilterMethod.Invoke(entityType, new object[] { nameof(IFullAuditedEntity.IsDeleted), softDeleteLambda });
                }
                else
                {
                    // EF Core 9 及以下兼容逻辑
                    entityType.SetQueryFilter(softDeleteLambda);
                }
            }
        }
    }
}
