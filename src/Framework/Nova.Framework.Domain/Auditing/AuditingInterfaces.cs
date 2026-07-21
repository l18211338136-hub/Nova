using System;

namespace Nova.Framework.Domain.Auditing;

public interface IAuditedEntity
{
    Guid? CreatedBy { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    Guid? ModifiedBy { get; set; }
    DateTimeOffset? ModifiedAt { get; set; }
}

public interface IFullAuditedEntity : IAuditedEntity
{
    Guid? DeletedBy { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    bool IsDeleted { get; set; }
    string? Remarks { get; set; }
    int Sort { get; set; }
    bool IsEnabled { get; set; }
}
