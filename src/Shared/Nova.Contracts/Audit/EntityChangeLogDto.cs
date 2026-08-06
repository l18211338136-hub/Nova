using System.ComponentModel;

namespace Nova.Contracts.Audit;

[Description("实体级别数据变更日志")]
public class EntityChangeLogDto
{
    [Description("日志标识")]
    public Guid Id { get; set; }

    [Description("实体类型")]
    public string EntityType { get; set; } = default!;

    [Description("实体主键")]
    public string EntityId { get; set; } = default!;

    [Description("变更类型")]
    public string ChangeType { get; set; } = default!; // Added, Modified, Deleted

    [Description("操作人标识")]
    public Guid? OperatorId { get; set; }

    [Description("操作人账号")]
    public string? OperatorName { get; set; }

    [Description("变更发生时间")]
    public DateTimeOffset CreatedAt { get; set; }

    [Description("变动字段明细")]
    public List<EntityPropertyChangeDto> PropertyChanges { get; set; } = new();
}
