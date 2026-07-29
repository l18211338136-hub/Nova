using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Framework.Web.Security;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Application.Menus.Queries;
using Nova.Modules.Identity.Application.Roles.Queries;
using Nova.Modules.Identity.Application.Users.Queries;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using System.ComponentModel;
using System.Reflection;

namespace Nova.Modules.Identity.Api;

public static class IdentityODataEndpoints
{
    public static void MapIdentityODataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/users", async (IIdentityDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.Users.ProjectToType<UserDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<UserDto>("Users");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(UserDto), null);
            var odataQuery = new ODataQueryOptions<UserDto>(odataContext, request);

            var filteredQuery = (IQueryable<UserDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

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

            var pagedResult = new PagedResult<UserDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<UserDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<UserDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Identity.Users.Read"))
        .WithTags("Users")
        .WithSummary("获取用户列表")
        .WithDescription("获取分页的用户列表数据");

        endpoints.MapGet("/api/identity/roles", async (IIdentityDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.Roles.ProjectToType<RoleDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<RoleDto>("Roles");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(RoleDto), null);
            var odataQuery = new ODataQueryOptions<RoleDto>(odataContext, request);

            var filteredQuery = (IQueryable<RoleDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

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

            var pagedResult = new PagedResult<RoleDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<RoleDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<RoleDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Identity.Roles.Read"))
        .WithTags("Roles")
        .WithSummary("获取角色列表")
        .WithDescription("获取分页的角色列表数据");

        endpoints.MapGet("/api/identity/menus", async (IIdentityDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.Menus.ProjectToType<MenuDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<MenuDto>("Menus");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(MenuDto), null);
            var odataQuery = new ODataQueryOptions<MenuDto>(odataContext, request);

            var filteredQuery = (IQueryable<MenuDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

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

            var pagedResult = new PagedResult<MenuDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<MenuDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<MenuDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Identity.Menus.Read"))
        .WithTags("Menus")
        .WithSummary("获取菜单列表")
        .WithDescription("获取分页的菜单列表数据");

        endpoints.MapGet("/api/identity/permissions/all", () =>
        {
            var apiPermissions = new HashSet<string>();
            var dllFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "Nova.*.dll");

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttribute<RequirePermissionAttribute>();
                        if (attr != null)
                        {
                            apiPermissions.Add(attr.Permission);
                        }
                    }
                }
                catch { }
            }
            return ApiResponse<List<string>>.Success(apiPermissions.ToList());
        })
        .Produces<ApiResponse<List<string>>>(200)
        .RequireAuthorization()
        .WithTags("Permissions")
        .WithSummary("获取系统所有可用权限")
        .WithName("GetAllPermissions");

        endpoints.MapGet("/api/identity/permissions/groups", () =>
        {
            var groups = new Dictionary<string, string>();
            var dllFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "Nova.*.dll");

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttribute<RequirePermissionAttribute>();
                        if (attr != null)
                        {
                            var parts = attr.Permission.Split('.');
                            var prefix = parts.Length > 1 ? $"{parts[0]}.{parts[1]}" : parts[0];

                            var descAttr = type.GetCustomAttribute<DescriptionAttribute>();
                            if (descAttr != null && !groups.ContainsKey(prefix))
                            {
                                groups[prefix] = descAttr.Description;
                            }
                        }
                    }
                }
                catch { }
            }
            return ApiResponse<Dictionary<string, string>>.Success(groups);
        })
        .Produces<ApiResponse<Dictionary<string, string>>>(200)
        .RequireAuthorization()
        .WithTags("Permissions")
        .WithSummary("获取权限分组名称映射表")
        .WithName("GetPermissionGroups");

        endpoints.MapGet("/api/identity/roles/{id}/permissions", async (string id, RoleManager<Role> roleManager) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role == null) return ApiResponse<List<string>>.Success(new List<string>());

            var claims = await roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

            return ApiResponse<List<string>>.Success(permissions);
        })
        .Produces<ApiResponse<List<string>>>(200)
        .RequireAuthorization()
        .WithTags("Roles")
        .WithSummary("获取某个角色的所有权限")
        .WithName("GetRolePermissions");

        endpoints.MapGet("/api/identity/users/{id}/roles", async (string id, UserManager<User> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return ApiResponse<List<string>>.Success(new List<string>());
            var roles = await userManager.GetRolesAsync(user);
            return ApiResponse<List<string>>.Success(roles.ToList());
        })
        .Produces<ApiResponse<List<string>>>(200)
        .RequireAuthorization()
        .WithTags("Users")
        .WithSummary("获取某个用户关联的角色列表")
        .WithName("GetUserRoles");

        endpoints.MapGet("/api/identity/users/{id}/permissions", async (string id, UserManager<User> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return ApiResponse<List<string>>.Success(new List<string>());
            var claims = await userManager.GetClaimsAsync(user);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
            return ApiResponse<List<string>>.Success(permissions);
        })
        .Produces<ApiResponse<List<string>>>(200)
        .RequireAuthorization()
        .WithTags("Users")
        .WithSummary("获取某个用户直接关联的权限列表")
        .WithName("GetUserPermissions");
    }
}
