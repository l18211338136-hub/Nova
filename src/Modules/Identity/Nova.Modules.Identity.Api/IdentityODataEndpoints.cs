using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Extensions;
using Nova.Modules.Identity.Infrastructure.Database;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Identity.Api;

public static class IdentityODataEndpoints
{
    public static void MapIdentityODataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/users", [EnableQuery] (IIdentityDbContext db) =>
        {
            return db.Users.ProjectToType<Application.Users.Queries.UserDto>();
        })
        .Produces<ApiResponse<ODataPagedResult<Application.Users.Queries.UserDto>>>(200)
        .AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            var odataFeature = context.HttpContext.Request.ODataFeature();
            long countVal = odataFeature.TotalCount ?? 0;

            var paged = new 
            {
                TotalCount = countVal,
                Items = result
            };
            
            return ApiResponse<object>.Success(paged);
        })
        .WithTags("Users OData Query");
    }
}

public record ODataPagedResult<T>
{
    public long TotalCount { get; init; }
    public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
}
