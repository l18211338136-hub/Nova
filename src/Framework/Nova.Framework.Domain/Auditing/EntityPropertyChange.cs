using Nova.Framework.Domain.Entities;

namespace Nova.Framework.Domain.Auditing;

[DisableEntityChangeAuditing]
public class EntityPropertyChange : Entity<Guid>
{
    public Guid EntityChangeLogId { get; private set; }
    public string PropertyName { get; private set; } = default!;
    public string? PropertyDisplayName { get; private set; }
    public string? OriginalValue { get; private set; }
    public string? NewValue { get; private set; }

    private EntityPropertyChange() { }

    public static EntityPropertyChange Create(
        Guid logId,
        string propertyName,
        string? originalValue,
        string? newValue,
        string? displayName = null)
    {
        return new EntityPropertyChange
        {
            Id = Guid.CreateVersion7(),
            EntityChangeLogId = logId,
            PropertyName = propertyName,
            PropertyDisplayName = displayName ?? propertyName,
            OriginalValue = originalValue,
            NewValue = newValue
        };
    }
}
