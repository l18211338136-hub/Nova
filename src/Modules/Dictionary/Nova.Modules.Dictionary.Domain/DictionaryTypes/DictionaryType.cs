using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Dictionary.Domain.DictionaryTypes;

public class DictionaryType : FullAuditedEntity<Guid>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private DictionaryType() { }

    public static DictionaryType Create(
        string code,
        string name,
        string? description = null,
        bool isSystem = false,
        bool isEnabled = true,
        int sort = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new DictionaryType
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystem = isSystem,
            IsEnabled = isEnabled,
            Sort = sort,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? description, bool isEnabled, int sort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description?.Trim();
        IsEnabled = isEnabled;
        Sort = sort;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
