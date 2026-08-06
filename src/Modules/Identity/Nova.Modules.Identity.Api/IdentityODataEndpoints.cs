using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Framework.Web.Security;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Application.Events;
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
        .WithTags("Users")
        .WithSummary("用户列表");

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
        .WithTags("Roles")
        .WithSummary("角色列表");

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
        .WithTags("Menus")
        .WithSummary("菜单列表");

        endpoints.MapGet("/api/identity/auth-audit-logs", async (IIdentityDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.AuthAuditLogs
                .OrderByDescending(l => l.OccurredOn)
                .Select(l => new AuthAuditLogDto
                {
                    Id = l.Id,
                    EventType = l.EventType,
                    Account = l.Account,
                    UserId = l.UserId,
                    Success = l.Success,
                    Reason = l.Reason,
                    IpAddress = l.IpAddress,
                    OccurredOn = l.OccurredOn,
                    CreatedAt = l.CreatedAt
                });

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<AuthAuditLogDto>("AuthAuditLogs");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(AuthAuditLogDto), null);
            var odataQuery = new ODataQueryOptions<AuthAuditLogDto>(odataContext, request);

            // 应用 $filter / $orderby / $count（忽略 Top/Skip，由下方手动分页）
            var filteredQuery = (IQueryable<AuthAuditLogDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

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

            var pagedResult = new PagedResult<AuthAuditLogDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<AuthAuditLogDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<AuthAuditLogDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Identity.AuditLogs.Read"))
        .WithTags("Audit")
        .WithSummary("审计日志");

        endpoints.MapGet("/api/identity/menus/me", async (IIdentityDbContext db, HttpContext httpContext, CancellationToken cancellationToken, UserManager<User> userManager, RoleManager<Role> roleManager) =>
        {
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<List<MenuDto>>.Success(new List<MenuDto>());
            }

            var menuClaims = new List<string>();
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var claims = await userManager.GetClaimsAsync(user);
                menuClaims.AddRange(claims.Where(c => c.Type == "Menu").Select(c => c.Value));

                var roles = await userManager.GetRolesAsync(user);
                foreach (var roleName in roles)
                {
                    var role = await roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var roleClaims = await roleManager.GetClaimsAsync(role);
                        menuClaims.AddRange(roleClaims.Where(c => c.Type == "Menu").Select(c => c.Value));
                    }
                }
            }

            menuClaims.AddRange(httpContext.User.Claims
                .Where(c => c.Type == "Menu" || c.Type == "menu" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/menu")
                .Select(c => c.Value));

            menuClaims = menuClaims.Distinct().ToList();

            var query = db.Menus.AsQueryable();

            if (menuClaims.Any())
            {
                var menuIds = menuClaims.Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty).Where(id => id != Guid.Empty).ToList();
                query = query.Where(m => menuIds.Contains(m.Id) || m.Path == "/");
            }
            else
            {
                query = query.Where(m => m.Path == "/");
            }

            var menus = await query.OrderBy(m => m.Sort).ProjectToType<MenuDto>().ToListAsync(cancellationToken);
            return ApiResponse<List<MenuDto>>.Success(menus);
        })
        .Produces<ApiResponse<List<MenuDto>>>(200)
        .RequireAuthorization()
        .WithTags("Menus")
        .WithSummary("用户菜单")
        .WithName("GetMyMenus");

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
        .WithSummary("可用权限")
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
        .WithSummary("权限映射")
        .WithName("GetPermissionGroups");

        endpoints.MapGet("/api/identity/roles/{id}/permissions", async (string id, RoleManager<Role> roleManager) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role == null) return ApiResponse<RolePermissionsDto>.Success(new RolePermissionsDto());

            var claims = await roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
            var menus = claims.Where(c => c.Type == "Menu").Select(c => c.Value).ToList();

            return ApiResponse<RolePermissionsDto>.Success(new RolePermissionsDto
            {
                Permissions = permissions,
                Menus = menus
            });
        })
        .Produces<ApiResponse<RolePermissionsDto>>(200)
        .RequireAuthorization()
        .WithTags("Roles")
        .WithSummary("角色权限")
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
        .WithSummary("用户角色")
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
        .WithSummary("用户权限")
        .WithName("GetUserPermissions");

        endpoints.MapGet("/api/identity/trash-bin", async (Nova.Contracts.TrashBin.ITrashBinService trashBinService, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var pagedResult = await trashBinService.GetDeletedItemsAsync(
                page: 1,
                pageSize: 1000,
                cancellationToken: cancellationToken);

            var query = pagedResult.Items.AsQueryable();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Nova.Contracts.TrashBin.TrashBinItemDto>("TrashBinItems");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(Nova.Contracts.TrashBin.TrashBinItemDto), null);
            var odataQuery = new ODataQueryOptions<Nova.Contracts.TrashBin.TrashBinItemDto>(odataContext, request);

            var filteredQuery = (IQueryable<Nova.Contracts.TrashBin.TrashBinItemDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

            long totalCount = filteredQuery.LongCount();

            if (odataQuery.Skip != null)
            {
                filteredQuery = filteredQuery.Skip(odataQuery.Skip.Value);
            }
            if (odataQuery.Top != null)
            {
                filteredQuery = filteredQuery.Take(odataQuery.Top.Value);
            }

            var items = filteredQuery.ToArray();

            int? top = odataQuery.Top?.Value;
            int? skip = odataQuery.Skip?.Value;
            int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;

            var result = new PagedResult<Nova.Contracts.TrashBin.TrashBinItemDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<Nova.Contracts.TrashBin.TrashBinItemDto>>.Success(result);
        })
        .Produces<ApiResponse<PagedResult<Nova.Contracts.TrashBin.TrashBinItemDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Identity.TrashBin.Read"))
        .WithTags("TrashBin")
        .WithSummary("回收列表")
        .WithName("GetTrashBinItems");
    }
}
