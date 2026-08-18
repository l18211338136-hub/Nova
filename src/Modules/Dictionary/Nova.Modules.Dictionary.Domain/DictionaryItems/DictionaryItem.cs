using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Dictionary.Domain.DictionaryItems;

public class DictionaryItem : FullAuditedEntity<Guid>
{
    public Guid TypeId { get; private set; }
    public string TypeCode { get; private set; } = default!;
    public string Label { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string? TagType { get; private set; } // default, primary, success, warning, danger
    public bool IsDefault { get; private set; }

    private DictionaryItem() { }

    public static DictionaryItem Create(
        Guid typeId,
        string typeCode,
        string label,
        string value,
        string? tagType = "default",
        int sort = 0,
        bool isDefault = false,
        bool isEnabled = true,
        string? remarks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new DictionaryItem
        {
            Id = Guid.CreateVersion7(),
            TypeId = typeId,
            TypeCode = typeCode.Trim().ToLowerInvariant(),
            Label = label.Trim(),
            Value = value.Trim(),
            TagType = tagType?.Trim() ?? "default",
            Sort = sort,
            IsDefault = isDefault,
            IsEnabled = isEnabled,
            Remarks = remarks?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string label,
        string value,
        string? tagType,
        int sort,
        bool isDefault,
        bool isEnabled,
        string? remarks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Label = label.Trim();
        Value = value.Trim();
        TagType = tagType?.Trim() ?? "default";
        Sort = sort;
        IsDefault = isDefault;
        IsEnabled = isEnabled;
        Remarks = remarks?.Trim();
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void SetTypeCode(string newTypeCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTypeCode);
        TypeCode = newTypeCode.Trim().ToLowerInvariant();
    }
}
