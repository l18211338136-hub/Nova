using System.ComponentModel;

namespace Nova.Contracts.TrashBin;

[Description("数据回收")]
public class TrashBinItemDto
{
    [Description("数据标识")]
    public Guid Id { get; set; }

    [Description("实体类型")]
    public string EntityType { get; set; } = default!;

    [Description("显示名称")]
    public string DisplayName { get; set; } = default!;

    [Description("删除用户ID")]
    public Guid? DeletedBy { get; set; }

    [Description("删除操作人账号")]
    public string? DeletedByUserName { get; set; }

    [Description("删除时间")]
    public DateTimeOffset? DeletedAt { get; set; }
}
