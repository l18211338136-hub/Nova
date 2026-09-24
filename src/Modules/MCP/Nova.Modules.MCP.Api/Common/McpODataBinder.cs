using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;

namespace Nova.Modules.Mcp.Api.Common;

/// <summary>
/// OData 协议绑定结果：已应用 $filter/$orderby 的查询 + 分页参数（$skip/$top）。
/// </summary>
public sealed record ODataBoundQuery<TDto>(IQueryable<TDto> Query, int? Skip, int? Top) where TDto : class;

/// <summary>
/// OData 请求参数绑定器（Web API 最外层职责：把 HTTP 协议参数翻译成查询）。
/// 数据源与分页执行由 Application 层契约 + Infrastructure 层实现负责，本类只做协议翻译。
/// </summary>
public static class McpODataBinder
{
    /// <summary>
    /// 将请求中的 OData 查询参数应用到给定的查询源。
    /// $skip/$top 不在此处应用（交由分页服务统一处理），故被忽略。
    /// </summary>
    public static ODataBoundQuery<TDto> Bind<TDto>(IQueryable<TDto> source, HttpRequest request)
        where TDto : class
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<TDto>(typeof(TDto).Name);
        var edmModel = builder.GetEdmModel();

        var odataContext = new ODataQueryContext(edmModel, typeof(TDto), null);
        var odataQuery = new ODataQueryOptions<TDto>(odataContext, request);

        var query = (IQueryable<TDto>)odataQuery.ApplyTo(
            source,
            ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        return new ODataBoundQuery<TDto>(query, odataQuery.Skip?.Value, odataQuery.Top?.Value);
    }

    /// <summary>
    /// 将契约层的分页结果转换为 Web 层的分页结果（保持对外 OpenAPI 契约不变）。
    /// </summary>
    public static Nova.Framework.Web.Responses.PagedResult<TDto> ToWebResult<TDto>(
        Nova.Contracts.Responses.PagedResult<TDto> source) where TDto : class
    {
        return new Nova.Framework.Web.Responses.PagedResult<TDto>
        {
            Total = source.Total,
            Items = source.Items,
            Page = source.Page,
            PageSize = source.PageSize
        };
    }
}
