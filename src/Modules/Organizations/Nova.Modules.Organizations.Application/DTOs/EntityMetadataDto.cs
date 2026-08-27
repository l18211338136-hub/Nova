namespace Nova.Modules.Organizations.Application.DTOs;

/// <summary>
/// 业务实体/数据表 ABAC 元数据 DTO
/// </summary>
public class EntityMetadataDto
{
    /// <summary>实体/表 Class 名称 (如 Order)</summary>
    public string EntityName { get; set; } = default!;

    /// <summary>实体/表 中文显示名 (如 订单业务表)</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>可配置属性字段元数据列表</summary>
    public List<EntityFieldMetadataDto> Fields { get; set; } = new();
}

/// <summary>
/// 业务实体属性字段元数据 DTO
/// </summary>
public class EntityFieldMetadataDto
{
    /// <summary>属性字段英文名 (如 Amount)</summary>
    public string Name { get; set; } = default!;

    /// <summary>属性字段中文显示名 (如 订单金额)</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>数据类型 (如 string, decimal, int, DateTime, Guid, bool)</summary>
    public string Type { get; set; } = "string";

    /// <summary>是否支持敏感打码脱敏</summary>
    public bool SupportMasking { get; set; } = true;
}
