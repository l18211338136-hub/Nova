using System;
using Microsoft.AspNetCore.Identity;
using Nova.Framework.Domain.Auditing;
using Nova.Modules.Identity.Domain.Menus;

namespace Nova.Modules.Identity.Domain.Roles;

public class Role : IdentityRole<Guid>, IFullAuditedEntity
{
    // 给 EF Core 使用的私有无参构造函数
    private Role()
    {
        Id = Guid.CreateVersion7();
    }

    // 强约束的私有全参构造函数
    private Role(string name, string displayName, string? remarks, int sort, bool isEnabled) : this()
    {
        Name = name;
        DisplayName = displayName;
        Remarks = remarks;
        Sort = sort;
        IsEnabled = isEnabled;
    }

    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string DisplayName { get; private set; } = default!;
    public string? Remarks { get; private set; }
    public int Sort { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    public static Role Create(string name, string displayName, string? remarks, int sort, bool isEnabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        return new Role(name, displayName, remarks, sort, isEnabled);
    }

    public void Update(string name, string displayName, string? remarks, int sort, bool isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        Name = name;
        DisplayName = displayName;
        Remarks = remarks;
        Sort = sort;
        IsEnabled = isEnabled;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}
