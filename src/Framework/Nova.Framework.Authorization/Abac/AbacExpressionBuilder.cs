using System.Linq.Expressions;
using System.Reflection;

namespace Nova.Framework.Authorization.Abac;

public static class AbacExpressionBuilder
{
    public static Expression<Func<T, bool>>? BuildRowLevelExpression<T>(
        List<AbacRuleConfig>? rules,
        string logic,
        Guid currentUserId,
        Guid? currentOrgId = null)
    {
        if (rules == null || !rules.Any())
            return null;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedBody = null;

        var isAnd = string.Equals(logic, "AND", StringComparison.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Field))
                continue;

            var property = typeof(T).GetProperty(rule.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                continue;

            var propAccess = Expression.Property(parameter, property);
            Expression? ruleExpr = BuildRuleExpression(propAccess, property.PropertyType, rule, currentUserId, currentOrgId);

            if (ruleExpr == null)
                continue;

            if (combinedBody == null)
            {
                combinedBody = ruleExpr;
            }
            else
            {
                combinedBody = isAnd
                    ? Expression.AndAlso(combinedBody, ruleExpr)
                    : Expression.OrElse(combinedBody, ruleExpr);
            }
        }

        if (combinedBody == null)
            return null;

        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    private static Expression? BuildRuleExpression(
        MemberExpression propAccess,
        Type propType,
        AbacRuleConfig rule,
        Guid currentUserId,
        Guid? currentOrgId)
    {
        var op = rule.Operator?.Trim().ToLowerInvariant() ?? "=";

        // 处理基础数据类型（包含 Nullable 类型解包）
        var targetType = Nullable.GetUnderlyingType(propType) ?? propType;

        if (op == "between")
        {
            var minVal = ResolveValue(rule.ValueMin, targetType, currentUserId, currentOrgId);
            var maxVal = ResolveValue(rule.ValueMax, targetType, currentUserId, currentOrgId);

            if (minVal == null || maxVal == null)
                return null;

            var minConst = Expression.Constant(minVal, propType);
            var maxConst = Expression.Constant(maxVal, propType);

            var gte = Expression.GreaterThanOrEqual(propAccess, minConst);
            var lte = Expression.LessThanOrEqual(propAccess, maxConst);

            return Expression.AndAlso(gte, lte);
        }

        if (op == "in")
        {
            if (string.IsNullOrWhiteSpace(rule.Value))
                return null;

            var rawValues = rule.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var listValues = rawValues
                .Select(v => ResolveValue(v, targetType, currentUserId, currentOrgId))
                .Where(v => v != null)
                .ToList();

            if (!listValues.Any())
                return null;

            var containsMethod = typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(targetType);

            var typedList = Array.CreateInstance(targetType, listValues.Count);
            for (int i = 0; i < listValues.Count; i++)
            {
                typedList.SetValue(listValues[i], i);
            }

            var listConst = Expression.Constant(typedList);
            var unboxedProp = propType != targetType ? Expression.Convert(propAccess, targetType) : (Expression)propAccess;

            return Expression.Call(containsMethod, listConst, unboxedProp);
        }

        var resolvedValue = ResolveValue(rule.Value, targetType, currentUserId, currentOrgId);
        if (resolvedValue == null && op != "=" && op != "!=")
            return null;

        var valConst = Expression.Constant(resolvedValue, propType);

        return op switch
        {
            "=" or "eq" => Expression.Equal(propAccess, valConst),
            "!=" or "neq" => Expression.NotEqual(propAccess, valConst),
            ">=" or "gte" => Expression.GreaterThanOrEqual(propAccess, valConst),
            "<=" or "lte" => Expression.LessThanOrEqual(propAccess, valConst),
            ">" or "gt" => Expression.GreaterThan(propAccess, valConst),
            "<" or "lt" => Expression.LessThan(propAccess, valConst),
            "like" or "contains" => BuildLikeExpression(propAccess, rule.Value),
            _ => Expression.Equal(propAccess, valConst)
        };
    }

    private static Expression BuildLikeExpression(MemberExpression propAccess, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Expression.Constant(true);

        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var valConst = Expression.Constant(rawValue);

        if (propAccess.Type != typeof(string))
        {
            var toStringMethod = propAccess.Type.GetMethod("ToString", Type.EmptyTypes);
            if (toStringMethod == null)
                return Expression.Constant(true);
            var strCall = Expression.Call(propAccess, toStringMethod);
            return Expression.Call(strCall, containsMethod!, valConst);
        }

        return Expression.Call(propAccess, containsMethod!, valConst);
    }

    private static object? ResolveValue(string? rawValue, Type targetType, Guid currentUserId, Guid? currentOrgId)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        // 动态宏替换
        if (rawValue.Equals(AbacConstants.DynamicMacros.CurrentUserId, StringComparison.OrdinalIgnoreCase))
        {
            if (targetType == typeof(Guid)) return currentUserId;
            return currentUserId.ToString();
        }

        if (rawValue.Equals(AbacConstants.DynamicMacros.CurrentOrgId, StringComparison.OrdinalIgnoreCase))
        {
            if (currentOrgId == null) return null;
            if (targetType == typeof(Guid)) return currentOrgId.Value;
            return currentOrgId.Value.ToString();
        }

        if (rawValue.Equals(AbacConstants.DynamicMacros.Recent30Days, StringComparison.OrdinalIgnoreCase))
        {
            var dt30 = DateTimeOffset.UtcNow.AddDays(-30);
            if (targetType == typeof(DateTimeOffset)) return dt30;
            if (targetType == typeof(DateTime)) return dt30.DateTime;
        }

        try
        {
            if (targetType == typeof(Guid)) return Guid.Parse(rawValue);
            if (targetType == typeof(int)) return int.Parse(rawValue);
            if (targetType == typeof(long)) return long.Parse(rawValue);
            if (targetType == typeof(bool)) return bool.Parse(rawValue);
            if (targetType == typeof(DateTimeOffset)) return DateTimeOffset.Parse(rawValue);
            if (targetType == typeof(DateTime)) return DateTime.Parse(rawValue);

            return Convert.ChangeType(rawValue, targetType);
        }
        catch
        {
            return rawValue;
        }
    }
}
