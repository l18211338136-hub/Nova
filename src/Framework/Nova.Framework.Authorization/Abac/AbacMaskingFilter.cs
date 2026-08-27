using Microsoft.AspNetCore.Http;

namespace Nova.Framework.Authorization.Abac;

/// <summary>
/// AOP 切面拦截器：自动拦截 API 响应结果，应用 ABAC 字段级隐藏与打码脱敏
/// </summary>
public class AbacMaskingFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result == null)
            return result;

        var fieldConfigs = context.HttpContext.Items[AbacConstants.HttpContextKeys.AbacFieldConfigs] as List<AbacFieldPermissionConfig>;
        if (fieldConfigs != null && fieldConfigs.Any())
        {
            object? dataToMask = result;
            var resultType = result.GetType();

            // 如果响应对象是包装结构（如 ApiResponse<T>），提取其 Data 属性
            var dataProp = resultType.GetProperty("Data");
            if (dataProp != null)
            {
                dataToMask = dataProp.GetValue(result);
            }

            if (dataToMask != null)
            {
                AbacFieldMasker.ApplyMaskingRecursive(dataToMask, fieldConfigs);
            }
        }

        return result;
    }
}
