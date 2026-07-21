using System;
using Microsoft.AspNetCore.Identity;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain.Roles;

public class Role : IdentityRole<Guid>, IFullAuditedEntity
{
    public Role()
    {
        Id = Guid.CreateVersion7();
    }

    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Remarks { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}
