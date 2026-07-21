using System;
using Nova.Framework.Domain.Entities;

namespace Nova.Framework.Domain.Auditing;

public abstract class FullAuditedEntity<TKey> : Entity<TKey>, IFullAuditedEntity
{
    public virtual Guid? CreatedBy { get; set; }
    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public virtual Guid? ModifiedBy { get; set; }
    public virtual DateTimeOffset? ModifiedAt { get; set; }
    public virtual Guid? DeletedBy { get; set; }
    public virtual DateTimeOffset? DeletedAt { get; set; }
    public virtual bool IsDeleted { get; set; }
    public virtual string? Remarks { get; set; }
    public virtual int Sort { get; set; }
    public virtual bool IsEnabled { get; set; } = true;
}
