using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nova.Framework.Authorization.Abac;

public static class AbacFieldMasker
{
    public static void ApplyMasking<T>(IEnumerable<T>? items, List<AbacFieldPermissionConfig>? fieldConfigs)
    {
        if (items == null || fieldConfigs == null || !fieldConfigs.Any())
            return;

        ApplyMaskingRecursive(items, fieldConfigs);
    }

    public static void ApplyMaskingRecursive(object? target, List<AbacFieldPermissionConfig>? fieldConfigs)
    {
        if (target == null || fieldConfigs == null || !fieldConfigs.Any())
            return;

        if (target is IEnumerable enumerable && target is not string)
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    MaskSingleObject(item, fieldConfigs);
                }
            }
        }
        else
        {
            MaskSingleObject(target, fieldConfigs);
        }
    }

    private static void MaskSingleObject(object item, List<AbacFieldPermissionConfig> fieldConfigs)
    {
        var type = item.GetType();
        var propMap = fieldConfigs.ToDictionary(f => f.Field, f => f, StringComparer.OrdinalIgnoreCase);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        foreach (var prop in properties)
        {
            if (propMap.TryGetValue(prop.Name, out var config))
            {
                if (config.Hide)
                {
                    prop.SetValue(item, GetDefaultValue(prop.PropertyType));
                }
                else if (config.Mask)
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        var rawVal = prop.GetValue(item) as string;
                        if (!string.IsNullOrEmpty(rawVal))
                        {
                            prop.SetValue(item, MaskValue(rawVal, prop.Name));
                        }
                    }
                    else
                    {
                        // 不能对非字符串类型赋值 "***"，降级为隐藏（赋予默认值）
                        prop.SetValue(item, GetDefaultValue(prop.PropertyType));
                    }
                }
            }
            else
            {
                // 如果属性是集合或复杂类型（如 Children 节点），递归向下处理
                if (prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    var childCollection = prop.GetValue(item);
                    if (childCollection != null)
                    {
                        ApplyMaskingRecursive(childCollection, fieldConfigs);
                    }
                }
            }
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            return null;

        return Activator.CreateInstance(type);
    }

    private static string MaskValue(string value, string propName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var name = propName.ToLowerInvariant();

        // 手机号打码
        if (name.Contains("phone") || name.Contains("mobile") || (value.Length == 11 && value.All(char.IsDigit)))
        {
            if (value.Length >= 11)
                return $"{value[..3]}****{value[7..]}";
            if (value.Length > 4)
                return $"{value[..2]}****{value[^2..]}";
        }

        // 邮箱打码
        if (name.Contains("email") || value.Contains('@'))
        {
            var parts = value.Split('@');
            if (parts.Length == 2 && parts[0].Length > 1)
            {
                var prefix = parts[0][0] + "***";
                return $"{prefix}@{parts[1]}";
            }
        }

        // 一般字符串 / 编码打码：保留前 2~3 位，中间全用 *** 替换
        if (value.Length > 6)
        {
            return $"{value[..3]}***{value[^2..]}";
        }
        if (value.Length > 2)
        {
            return $"{value[..1]}***";
        }

        return "***";
    }
}
