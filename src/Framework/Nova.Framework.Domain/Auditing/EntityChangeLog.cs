using Nova.Framework.Domain.Entities;

namespace Nova.Framework.Domain.Auditing;

[DisableEntityChangeAuditing]
public class EntityChangeLog : Entity<Guid>
{
    public string? TenantId { get; private set; }
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string ChangeType { get; private set; } = default!; // Added, Modified, Deleted
    public Guid? OperatorId { get; private set; }
    public string? OperatorName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<EntityPropertyChange> _propertyChanges = new();
    public IReadOnlyCollection<EntityPropertyChange> PropertyChanges => _propertyChanges.AsReadOnly();

    private EntityChangeLog() { }

    public static EntityChangeLog Create(
        string entityType,
        string entityId,
        string changeType,
        Guid? operatorId = null,
        string? operatorName = null,
        string? tenantId = null)
    {
        return new EntityChangeLog
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            ChangeType = changeType,
            OperatorId = operatorId,
            OperatorName = operatorName ?? (operatorId.HasValue ? operatorId.Value.ToString() : "System"),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddPropertyChange(string propertyName, string? originalValue, string? newValue, string? displayName = null)
    {
        _propertyChanges.Add(EntityPropertyChange.Create(Id, propertyName, originalValue, newValue, displayName));
    }
}
