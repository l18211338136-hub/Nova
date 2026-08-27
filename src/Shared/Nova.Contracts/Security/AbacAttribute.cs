namespace Nova.Contracts.Security;

/// <summary>
/// 标记此类/实体参与 ABAC 属性数据权限控制
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AbacEntityAttribute : Attribute
{
    public string DisplayName { get; }
    
    public AbacEntityAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

/// <summary>
/// 标记此属性/字段可用于 ABAC 数据权限过滤及列隐藏/脱敏配置
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AbacFieldAttribute : Attribute
{
    public string DisplayName { get; }
    public bool SupportMasking { get; }

    public AbacFieldAttribute(string displayName, bool supportMasking = true)
    {
        DisplayName = displayName;
        SupportMasking = supportMasking;
    }
}
