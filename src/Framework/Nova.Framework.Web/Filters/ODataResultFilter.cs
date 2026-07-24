using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Extensions;
using Nova.Framework.Web.Responses;

namespace Nova.Framework.Web.Filters;

public class ODataResultFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        // 如果结果已经是 ApiResponse，说明内部已经包装过了，直接返回
        if (result != null && result.GetType().IsGenericType && result.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            return result;
        }

        var odataFeature = context.HttpContext.Request.ODataFeature();
        long totalCount = odataFeature.TotalCount ?? 0;

        int? top = null;
        int? skip = null;
        if (int.TryParse(context.HttpContext.Request.Query["$top"], out int t)) top = t;
        if (int.TryParse(context.HttpContext.Request.Query["$skip"], out int s)) skip = s;

        int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;
        int? pageSize = top ?? 0;

        var pagedResult = new PagedResult<object>
        {
            Total = totalCount,
            Items = result as IEnumerable<object> ?? Array.Empty<object>(),
            Page = page,
            PageSize = pageSize > 0 ? pageSize : null
        };

        return ApiResponse<PagedResult<object>>.Success(pagedResult);
    }
}
