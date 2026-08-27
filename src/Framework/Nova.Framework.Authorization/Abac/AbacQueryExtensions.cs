using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Constants;
using Nova.Contracts.Security;

namespace Nova.Framework.Authorization.Abac;

public static class AbacQueryExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IQueryable<TEntity>> ApplyAbacFilterAsync<TEntity>(
        this IQueryable<TEntity> query,
        HttpContext httpContext,
        DbContext dbContext,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.User.FindFirst("user_id")?.Value;
        var currentUserId = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Empty;

        // 提取用户包含的所有部门 Claims（支持兼任多部门）
        var userOrgIds = httpContext.User.FindAll(NovaClaimTypes.OrgId)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        // 自动检索部门配置的 ABAC 行列防护策略
        var policyEntityType = dbContext.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.GetProperty(AbacConstants.PropertyNames.AbacPoliciesJson) != null);

        if (policyEntityType != null)
        {
            var clrType = policyEntityType.ClrType;
            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!.MakeGenericMethod(clrType);
            var dbSetObj = setMethod.Invoke(dbContext, null);

            if (dbSetObj is IQueryable<object> policyQuery)
            {
                var idProp = clrType.GetProperty(AbacConstants.PropertyNames.Id);
                var orgList = await policyQuery.AsNoTracking().ToListAsync(cancellationToken);
                var activeOrgObj = orgList.FirstOrDefault(x => idProp?.GetValue(x) is Guid id && userOrgIds.Contains(id));

                if (activeOrgObj != null)
                {
                    var policiesJsonProp = clrType.GetProperty(AbacConstants.PropertyNames.AbacPoliciesJson);
                    var dataScopeProp = clrType.GetProperty(AbacConstants.PropertyNames.DataScope);

                    var policiesJson = policiesJsonProp?.GetValue(activeOrgObj) as string;
                    var dataScope = dataScopeProp?.GetValue(activeOrgObj) is int ds ? ds : 0;

                    query = query.ApplyAbacFilter(httpContext, policiesJson, dataScope, currentUserId, userOrgIds);
                }
            }
        }

        return query;
    }

    public static IQueryable<T> ApplyAbacFilter<T>(
        this IQueryable<T> query,
        HttpContext httpContext,
        string? abacPoliciesJson,
        int dataScope,
        Guid currentUserId,
        List<Guid>? userOrgIds)
    {
        query = query.ApplyAbacFilter(abacPoliciesJson, dataScope, currentUserId, userOrgIds, out var fieldConfigs);
        if (fieldConfigs.Any())
        {
            httpContext.Items[AbacConstants.HttpContextKeys.AbacFieldConfigs] = fieldConfigs;
        }
        return query;
    }

    public static IQueryable<T> ApplyAbacFilter<T>(
        this IQueryable<T> query,
        string? abacPoliciesJson,
        int dataScope,
        Guid currentUserId,
        List<Guid>? userOrgIds,
        out List<AbacFieldPermissionConfig> fieldConfigs)
    {
        fieldConfigs = new List<AbacFieldPermissionConfig>();

        if (string.IsNullOrWhiteSpace(abacPoliciesJson) && dataScope == 0)
            return query;

        // 1. 数据范围基础条件处理 (DataScope)
        query = ApplyDataScopeFilter(query, dataScope, currentUserId, userOrgIds);

        // 2. ABAC 动态表达式解析 (Row-Level Rules & Fields)
        if (!string.IsNullOrWhiteSpace(abacPoliciesJson))
        {
            try
            {
                var policies = JsonSerializer.Deserialize<List<AbacPolicyConfig>>(abacPoliciesJson, JsonOptions);
                if (policies != null && policies.Any())
                {
                    // 100% 动态实体名称解包匹配（自动剥离 Dto/Tree 后缀，并支持 [AbacEntity] 特性）
                    var entityName = typeof(T).Name;
                    var cleanName = entityName.EndsWith("Dto", StringComparison.OrdinalIgnoreCase) ? entityName[..^3] : entityName;
                    cleanName = cleanName.EndsWith("Tree", StringComparison.OrdinalIgnoreCase) ? cleanName[..^4] : cleanName;

                    var abacAttr = typeof(T).GetCustomAttribute<AbacEntityAttribute>();

                    var targetPolicy = policies.FirstOrDefault(p =>
                        string.Equals(p.EntityName, entityName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.EntityName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                        (abacAttr != null && string.Equals(p.EntityName, abacAttr.DisplayName, StringComparison.OrdinalIgnoreCase))
                    );

                    if (targetPolicy != null)
                    {
                        if (targetPolicy.Fields != null)
                        {
                            fieldConfigs = targetPolicy.Fields;
                        }

                        var primaryOrgId = userOrgIds?.FirstOrDefault();
                        var rowExpression = AbacExpressionBuilder.BuildRowLevelExpression<T>(
                            targetPolicy.Rules,
                            targetPolicy.Logic,
                            currentUserId,
                            primaryOrgId
                        );

                        if (rowExpression != null)
                        {
                            query = query.Where(rowExpression);
                        }
                    }
                }
            }
            catch
            {
                // 静默忽略非法 JSON 配置，避免阻塞基础查询
            }
        }

        return query;
    }

    private static IQueryable<T> ApplyDataScopeFilter<T>(
        IQueryable<T> query,
        int dataScope,
        Guid currentUserId,
        List<Guid>? userOrgIds)
    {
        var type = typeof(T);
        var parameter = Expression.Parameter(type, "x");

        // DataScope: 1 (仅本人数据)
        if (dataScope == 1)
        {
            var leaderProp = type.GetProperty(AbacConstants.PropertyNames.LeaderUserId, BindingFlags.Public | BindingFlags.Instance) ??
                             type.GetProperty(AbacConstants.PropertyNames.CreatedBy, BindingFlags.Public | BindingFlags.Instance) ??
                             type.GetProperty(AbacConstants.PropertyNames.UserId, BindingFlags.Public | BindingFlags.Instance);

            if (leaderProp != null)
            {
                var propAccess = Expression.Property(parameter, leaderProp);
                var targetType = Nullable.GetUnderlyingType(leaderProp.PropertyType) ?? leaderProp.PropertyType;

                Expression constExpr = targetType == typeof(Guid)
                    ? Expression.Constant(currentUserId, leaderProp.PropertyType)
                    : Expression.Constant(currentUserId.ToString(), leaderProp.PropertyType);

                var lambda = Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(propAccess, constExpr),
                    parameter
                );

                return query.Where(lambda);
            }
        }

        // DataScope: 2 (仅本部门及兼任部门数据 - 支持多部门 IN 查询)
        if (dataScope == 2 && userOrgIds != null && userOrgIds.Any())
        {
            var orgIdProp = type.GetProperty(AbacConstants.PropertyNames.Id, BindingFlags.Public | BindingFlags.Instance) ??
                            type.GetProperty(AbacConstants.PropertyNames.OrganizationId, BindingFlags.Public | BindingFlags.Instance);

            if (orgIdProp != null)
            {
                var propAccess = Expression.Property(parameter, orgIdProp);
                var targetType = Nullable.GetUnderlyingType(orgIdProp.PropertyType) ?? orgIdProp.PropertyType;

                if (userOrgIds.Count == 1)
                {
                    var userOrgId = userOrgIds.First();
                    Expression constExpr = targetType == typeof(Guid)
                        ? Expression.Constant(userOrgId, orgIdProp.PropertyType)
                        : Expression.Constant(userOrgId.ToString(), orgIdProp.PropertyType);

                    var lambda = Expression.Lambda<Func<T, bool>>(
                        Expression.Equal(propAccess, constExpr),
                        parameter
                    );

                    return query.Where(lambda);
                }
                else
                {
                    var containsMethod = typeof(Enumerable)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(targetType);

                    var typedList = Array.CreateInstance(targetType, userOrgIds.Count);
                    for (int i = 0; i < userOrgIds.Count; i++)
                    {
                        typedList.SetValue(targetType == typeof(Guid) ? userOrgIds[i] : userOrgIds[i].ToString(), i);
                    }

                    var listConst = Expression.Constant(typedList);
                    var unboxedProp = orgIdProp.PropertyType != targetType ? Expression.Convert(propAccess, targetType) : (Expression)propAccess;
                    var containsCall = Expression.Call(containsMethod, listConst, unboxedProp);

                    var lambda = Expression.Lambda<Func<T, bool>>(containsCall, parameter);
                    return query.Where(lambda);
                }
            }
        }

        return query;
    }
}
