using System.ComponentModel;

namespace Nova.Contracts.Audit;

[Description("属性级别变动明细")]
public class EntityPropertyChangeDto
{
    [Description("明细标识")]
    public Guid Id { get; set; }

    [Description("属性字段名称")]
    public string PropertyName { get; set; } = default!;

    [Description("属性显示名称")]
    public string? PropertyDisplayName { get; set; }

    [Description("修改前原始值")]
    public string? OriginalValue { get; set; }

    [Description("修改后最新值")]
    public string? NewValue { get; set; }
}
