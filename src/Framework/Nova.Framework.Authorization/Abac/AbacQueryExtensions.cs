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
        ICurrentUser currentUser,
        DbContext dbContext,
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var currentUserId = currentUser.Id ?? Guid.Empty;

        // 提取用户包含的所有部门 Claims（支持兼任多部门）
        var userOrgIds = currentUser.GetClaimValues(NovaClaimTypes.OrgId)?
            .Select(c => Guid.TryParse(c, out var id) ? id : Guid.Empty)
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

                    var userOrgAndSubIds = new HashSet<Guid>(userOrgIds ?? new List<Guid>());
                    var parentIdProp = clrType.GetProperty("ParentId");
                    
                    bool added;
                    do
                    {
                        added = false;
                        foreach (var org in orgList)
                        {
                            var orgId = idProp?.GetValue(org) as Guid?;
                            var parentId = parentIdProp?.GetValue(org) as Guid?;

                            if (orgId.HasValue && parentId.HasValue && userOrgAndSubIds.Contains(parentId.Value) && !userOrgAndSubIds.Contains(orgId.Value))
                            {
                                userOrgAndSubIds.Add(orgId.Value);
                                added = true;
                            }
                        }
                    } while (added);

                    query = query.ApplyAbacFilter(currentUser, policiesJson, dataScope, currentUserId, userOrgIds, userOrgAndSubIds.ToList(), httpContext);
                }
            }
        }

        return query;
    }

    public static IQueryable<T> ApplyAbacFilter<T>(
        this IQueryable<T> query,
        ICurrentUser currentUser,
        string? abacPoliciesJson,
        int dataScope,
        Guid currentUserId,
        List<Guid>? userOrgIds,
        List<Guid>? userOrgAndSubIds = null,
        HttpContext? httpContext = null)
    {
        query = query.ApplyAbacFilter(abacPoliciesJson, dataScope, currentUserId, userOrgIds, userOrgAndSubIds, out var fieldConfigs);
        if (fieldConfigs.Any() && httpContext != null)
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
        List<Guid>? userOrgAndSubIds,
        out List<AbacFieldPermissionConfig> fieldConfigs)
    {
        fieldConfigs = new List<AbacFieldPermissionConfig>();

        if (string.IsNullOrWhiteSpace(abacPoliciesJson) && dataScope == 0)
            return query;

        // 1. 数据范围基础条件处理 (DataScope)
        query = ApplyDataScopeFilter(query, dataScope, currentUserId, userOrgIds, userOrgAndSubIds);

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
                            primaryOrgId,
                            userOrgAndSubIds
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
        List<Guid>? userOrgIds,
        List<Guid>? userOrgAndSubIds)
    {
        var type = typeof(T);
        var parameter = Expression.Parameter(type, "x");

        // DataScope: 4 (仅本人数据)
        if (dataScope == 4)
        {
            var conditions = new List<Expression>();

            foreach (var propertyName in new[]
            {
                AbacConstants.PropertyNames.LeaderUserId,
                AbacConstants.PropertyNames.CreatedBy,
                AbacConstants.PropertyNames.UserId
            })
            {
                var prop = type.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance);

                if (prop == null)
                    continue;

                var property = Expression.Property(parameter, prop);

                Expression currentUserExpression;

                if (prop.PropertyType == typeof(Guid?))
                {
                    currentUserExpression =
                        Expression.Constant((Guid?)currentUserId, typeof(Guid?));
                }
                else if (prop.PropertyType == typeof(Guid))
                {
                    currentUserExpression =
                        Expression.Constant(currentUserId, typeof(Guid));
                }
                else
                {
                    continue;
                }

                conditions.Add(
                    Expression.Equal(property, currentUserExpression));
            }

            if (conditions.Count == 0)
                return query;

            Expression body = conditions[0];

            foreach (var condition in conditions.Skip(1))
            {
                body = Expression.OrElse(body, condition);
            }

            var lambda = Expression.Lambda<Func<T, bool>>(
                body,
                parameter);

            return query.Where(lambda);
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

        // DataScope: 3 (本部门及下级部门数据)
        if (dataScope == 3 && userOrgAndSubIds != null && userOrgAndSubIds.Any())
        {
            var orgIdProp = type.GetProperty(AbacConstants.PropertyNames.Id, BindingFlags.Public | BindingFlags.Instance) ??
                            type.GetProperty(AbacConstants.PropertyNames.OrganizationId, BindingFlags.Public | BindingFlags.Instance);

            if (orgIdProp != null)
            {
                var propAccess = Expression.Property(parameter, orgIdProp);
                var targetType = Nullable.GetUnderlyingType(orgIdProp.PropertyType) ?? orgIdProp.PropertyType;

                if (userOrgAndSubIds.Count == 1)
                {
                    var userOrgId = userOrgAndSubIds.First();
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

                    var typedList = Array.CreateInstance(targetType, userOrgAndSubIds.Count);
                    for (int i = 0; i < userOrgAndSubIds.Count; i++)
                    {
                        typedList.SetValue(targetType == typeof(Guid) ? userOrgAndSubIds[i] : userOrgAndSubIds[i].ToString(), i);
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
