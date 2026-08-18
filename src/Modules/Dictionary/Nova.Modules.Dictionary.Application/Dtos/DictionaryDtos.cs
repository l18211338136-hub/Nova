namespace Nova.Modules.Dictionary.Application.Dtos;

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
