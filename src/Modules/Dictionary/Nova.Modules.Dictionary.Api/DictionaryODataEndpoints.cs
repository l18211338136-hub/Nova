using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Nova.Framework.Web.Responses;
using Nova.Framework.Web.Security;
using Nova.Modules.Dictionary.Application.Database;
using Nova.Modules.Dictionary.Application.Dtos;

namespace Nova.Modules.Dictionary.Api;

public static class DictionaryODataEndpoints
{
    public static void MapDictionaryODataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 1. 字典类型列表（支持 OData 分页与过滤）
        endpoints.MapGet("/api/dictionary/types", async (IDictionaryDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.DictionaryTypes.AsNoTracking().ProjectToType<DictionaryTypeDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<DictionaryTypeDto>("DictionaryTypes");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(DictionaryTypeDto), null);
            var odataQuery = new ODataQueryOptions<DictionaryTypeDto>(odataContext, request);

            var filteredQuery = (IQueryable<DictionaryTypeDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

            long totalCount = await filteredQuery.LongCountAsync(cancellationToken);

            if (odataQuery.Skip != null)
            {
                filteredQuery = filteredQuery.Skip(odataQuery.Skip.Value);
            }
            if (odataQuery.Top != null)
            {
                filteredQuery = filteredQuery.Take(odataQuery.Top.Value);
            }

            var items = await filteredQuery.ToArrayAsync(cancellationToken);

            int? top = odataQuery.Top?.Value;
            int? skip = odataQuery.Skip?.Value;
            int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;

            var pagedResult = new PagedResult<DictionaryTypeDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<DictionaryTypeDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<DictionaryTypeDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Dictionary.Types.Read"))
        .WithTags("Dictionaries")
        .WithSummary("字典类型列表")
        .WithName("GetDictionaryTypes");

        // 2. 字典明细项列表（支持 OData 分页与过滤）
        endpoints.MapGet("/api/dictionary/items", async (IDictionaryDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.DictionaryItems.AsNoTracking().ProjectToType<DictionaryItemDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<DictionaryItemDto>("DictionaryItems");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(DictionaryItemDto), null);
            var odataQuery = new ODataQueryOptions<DictionaryItemDto>(odataContext, request);

            var filteredQuery = (IQueryable<DictionaryItemDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

            long totalCount = await filteredQuery.LongCountAsync(cancellationToken);

            if (odataQuery.Skip != null)
            {
                filteredQuery = filteredQuery.Skip(odataQuery.Skip.Value);
            }
            if (odataQuery.Top != null)
            {
                filteredQuery = filteredQuery.Take(odataQuery.Top.Value);
            }

            var items = await filteredQuery.ToArrayAsync(cancellationToken);

            int? top = odataQuery.Top?.Value;
            int? skip = odataQuery.Skip?.Value;
            int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;

            var pagedResult = new PagedResult<DictionaryItemDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<DictionaryItemDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<DictionaryItemDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Dictionary.Items.Read"))
        .WithTags("Dictionaries")
        .WithSummary("字典数据项列表")
        .WithName("GetDictionaryItems");

        // 3. 全局通用的指定 code 字典下拉数据快捷获取接口 (无需复杂权限控制，仅需登录)
        endpoints.MapGet("/api/dictionary/items/code/{code}", async (string code, IDictionaryDbContext db, CancellationToken cancellationToken) =>
        {
            var codeLower = code.Trim().ToLowerInvariant();

            var items = await db.DictionaryItems
                .AsNoTracking()
                .Where(x => x.TypeCode == codeLower && x.IsEnabled)
                .OrderBy(x => x.Sort)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new DictionaryItemDto
                {
                    Id = x.Id,
                    TypeId = x.TypeId,
                    TypeCode = x.TypeCode,
                    Label = x.Label,
                    Value = x.Value,
                    TagType = x.TagType,
                    SortOrder = x.Sort,
                    IsDefault = x.IsDefault,
                    IsEnabled = x.IsEnabled,
                    Remarks = x.Remarks,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<DictionaryItemDto>>.Success(items);
        })
        .Produces<ApiResponse<List<DictionaryItemDto>>>(200)
        .RequireAuthorization()
        .WithTags("Dictionaries")
        .WithSummary("获取指定编码的字典选项")
        .WithName("GetDictionaryItemsByCode");
    }
}
