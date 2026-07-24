using Mapster;
using Microsoft.AspNetCore.OData.Query;
using Nova.Framework.Web.Filters;
using Nova.Framework.Web.Responses;
using Nova.Modules.Identity.Application.Users.Queries;
using Nova.Modules.Identity.Infrastructure.Database;

namespace Nova.Modules.Identity.Api;

public static class IdentityODataEndpoints
{
    public static void MapIdentityODataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/users", [EnableQuery] (IIdentityDbContext db) =>
        {
            return db.Users.ProjectToType<UserDto>();
        })
        .Produces<ApiResponse<PagedResult<UserDto>>>(200)
        .AddEndpointFilter<ODataResultFilter>()
        .WithTags("用户管理");
    }
}
