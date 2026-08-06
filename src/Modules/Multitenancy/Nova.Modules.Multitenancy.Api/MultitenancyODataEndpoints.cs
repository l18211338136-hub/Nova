using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Web.Responses;
using Nova.Framework.Web.Security;
using Nova.Modules.Multitenancy.Application.Queries;

namespace Nova.Modules.Multitenancy.Api;

public static class MultitenancyODataEndpoints
{
    public static void MapMultitenancyODataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/tenants", async (NovaTenantDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.TenantInfo.ProjectToType<TenantDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TenantDto>("Tenants");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(TenantDto), null);
            var odataQuery = new ODataQueryOptions<TenantDto>(odataContext, request);

            var filteredQuery = (IQueryable<TenantDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

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

            var pagedResult = new PagedResult<TenantDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<TenantDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<TenantDto>>>(200)
        .RequireAuthorization()
        .WithTags("Tenants")
        .WithSummary("租户列表");
    }
}
