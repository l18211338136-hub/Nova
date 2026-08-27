using System.Reflection;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Nova.Contracts.Security;
using Nova.Framework.Authorization.Abac;
using Nova.Framework.Web.Responses;
using Nova.Modules.Organizations.Application.Database;
using Nova.Modules.Organizations.Application.DTOs;

namespace Nova.Modules.Organizations.Api;

public static class OrganizationODataEndpoints
{
    public static void MapOrganizationODataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 1. 获取组织机构树状结构 (GET /api/organizations/tree)
        endpoints.MapGet("/api/organizations/tree", async (IOrganizationDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var query = await db.Organizations.AsNoTracking().ApplyAbacFilterAsync(currentUser, (DbContext)db, cancellationToken);

            var orgs = await query
                .OrderBy(o => o.Sort)
                .ThenBy(o => o.Name)
                .ToListAsync(cancellationToken);

            var memberCounts = await db.UserOrganizations
                .AsNoTracking()
                .GroupBy(uo => uo.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrganizationId, x => x.Count, cancellationToken);

            List<OrganizationTreeDto> BuildTree(Guid? parentId)
            {
                return orgs
                    .Where(o => o.ParentId == parentId)
                    .Select(o => new OrganizationTreeDto
                    {
                        Id = o.Id,
                        ParentId = o.ParentId,
                        Name = o.Name,
                        Code = o.Code,
                        Type = o.Type,
                        Level = o.Level,
                        Sort = o.Sort,
                        IsEnabled = o.IsEnabled,
                        MemberCount = memberCounts.GetValueOrDefault(o.Id, 0),
                        Children = BuildTree(o.Id)
                    })
                    .ToList();
            }

            var tree = BuildTree(null);
            return ApiResponse<List<OrganizationTreeDto>>.Success(tree);
        })
        .Produces<ApiResponse<List<OrganizationTreeDto>>>(200)
        .RequireAuthorization()
        .AddEndpointFilter<AbacMaskingFilter>()
        .WithTags("Organizations")
        .WithSummary("组织树图")
        .WithName("GetOrganizationTree");

        // 2. 获取组织机构列表 (支持 OData 过滤/分页: GET /api/organizations)
        endpoints.MapGet("/api/organizations", async (IOrganizationDbContext db, HttpRequest request, CancellationToken cancellationToken) =>
        {
            var query = db.Organizations.AsNoTracking().ProjectToType<OrganizationDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<OrganizationDto>("Organizations");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(OrganizationDto), null);
            var odataQuery = new ODataQueryOptions<OrganizationDto>(odataContext, request);

            var filteredQuery = (IQueryable<OrganizationDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

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

            var pagedResult = new PagedResult<OrganizationDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return ApiResponse<PagedResult<OrganizationDto>>.Success(pagedResult);
        })
        .Produces<ApiResponse<PagedResult<OrganizationDto>>>(200)
        .RequireAuthorization()
        .WithTags("Organizations")
        .WithSummary("组织列表")
        .WithName("GetOrganizations");

        // 3. 获取单条机构详情 (GET /api/organizations/{id})
        endpoints.MapGet("/api/organizations/{id:guid}", async (Guid id, IOrganizationDbContext db, CancellationToken cancellationToken) =>
        {
            var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (org == null)
            {
                return Results.NotFound(ApiResponse<OrganizationDto>.Error("组织机构不存在"));
            }

            var dto = org.Adapt<OrganizationDto>();
            dto.MemberCount = await db.UserOrganizations.CountAsync(uo => uo.OrganizationId == id, cancellationToken);

            return Results.Ok(ApiResponse<OrganizationDto>.Success(dto));
        })
        .Produces<ApiResponse<OrganizationDto>>(200)
        .RequireAuthorization()
        .WithTags("Organizations")
        .WithSummary("组织详情")
        .WithName("GetOrganizationById");

        // 4. 查询机构成员列表 (GET /api/organizations/{id}/members)
        endpoints.MapGet("/api/organizations/{id:guid}/members", async (Guid id, IOrganizationDbContext db, CancellationToken cancellationToken) =>
        {
            var userOrgs = await db.UserOrganizations
                .AsNoTracking()
                .Where(uo => uo.OrganizationId == id)
                .ToListAsync(cancellationToken);

            var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            var members = userOrgs.Select(uo => new OrganizationMemberDto
            {
                UserId = uo.UserId,
                OrganizationId = uo.OrganizationId,
                UserName = $"User_{uo.UserId.ToString()[..8]}",
                IsPrimary = uo.IsPrimary,
                JobTitle = uo.JobTitle,
                IsLeader = org?.LeaderUserId == uo.UserId,
                JoinedAt = uo.CreatedAt
            }).ToList();

            return ApiResponse<List<OrganizationMemberDto>>.Success(members);
        })
        .Produces<ApiResponse<List<OrganizationMemberDto>>>(200)
        .RequireAuthorization()
        .WithTags("Organizations")
        .WithSummary("成员列表")
        .WithName("GetOrganizationMembers");

        // 5. 获取部门权限与数据范围 (GET /api/organizations/{id}/permissions)
        endpoints.MapGet("/api/organizations/{id:guid}/permissions", async (Guid id, IOrganizationDbContext db, CancellationToken cancellationToken) =>
        {
            var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (org == null)
            {
                return Results.NotFound(ApiResponse<OrganizationPermissionsDto>.Error("组织机构不存在"));
            }

            var dto = new OrganizationPermissionsDto
            {
                OrganizationId = id,
                DataScope = org.DataScope,
                AbacPoliciesJson = org.AbacPoliciesJson
            };

            return Results.Ok(ApiResponse<OrganizationPermissionsDto>.Success(dto));
        })
        .Produces<ApiResponse<OrganizationPermissionsDto>>(200)
        .RequireAuthorization()
        .WithTags("Organizations")
        .WithSummary("部门权限")
        .WithName("GetOrganizationPermissions");

        // 6. 获取 ABAC 实体与属性列反射元数据 (GET /api/organizations/metadata/entities)
        endpoints.MapGet("/api/organizations/metadata/entities", () =>
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var entityMetadataList = new List<EntityMetadataDto>();

            foreach (var asm in assemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    var entityAttr = type.GetCustomAttribute<AbacEntityAttribute>();
                    if (entityAttr == null) continue;

                    var fieldDtos = new List<EntityFieldMetadataDto>();
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        var fieldAttr = prop.GetCustomAttribute<AbacFieldAttribute>();
                        if (fieldAttr != null)
                        {
                            fieldDtos.Add(new EntityFieldMetadataDto
                            {
                                Name = prop.Name,
                                DisplayName = fieldAttr.DisplayName,
                                Type = prop.PropertyType.Name,
                                SupportMasking = fieldAttr.SupportMasking
                            });
                        }
                    }

                    entityMetadataList.Add(new EntityMetadataDto
                    {
                        EntityName = type.Name,
                        DisplayName = entityAttr.DisplayName,
                        Fields = fieldDtos
                    });
                }
            }

            return ApiResponse<List<EntityMetadataDto>>.Success(entityMetadataList);
        })
        .Produces<ApiResponse<List<EntityMetadataDto>>>(200)
        .RequireAuthorization()
        .WithTags("Organizations")
        .WithSummary("获取 ABAC 实体元数据")
        .WithName("GetAbacEntityMetadata");
    }
}
