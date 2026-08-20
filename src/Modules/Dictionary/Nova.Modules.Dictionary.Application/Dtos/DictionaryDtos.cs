using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Dictionary.Application.Dtos;

[RequirePermission("Dictionary.Types.Read")]
[Description("字典类型")]
public class DictionaryTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

[RequirePermission("Dictionary.Items.Read")]
[Description("字典数据")]
public class DictionaryItemDto
{
    public Guid Id { get; set; }
    public Guid TypeId { get; set; }
    public string TypeCode { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string? TagType { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
