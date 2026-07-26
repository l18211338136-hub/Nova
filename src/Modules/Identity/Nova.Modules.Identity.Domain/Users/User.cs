using System;
using Microsoft.AspNetCore.Identity;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain.Users;

public class User : IdentityUser<Guid>, IFullAuditedEntity
{
    // 给 EF Core 使用的私有无参构造函数
    private User()
    {
        Id = Guid.CreateVersion7();
    }

    // 强约束的私有构造函数
    private User(string userName, string email, string? remarks = null, int sort = 0) : this()
    {
        UserName = userName;
        Email = email;
        EmailConfirmed = true;
        IsEnabled = true;
        Remarks = remarks;
        Sort = sort;
    }

    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Remarks { get; private set; }
    public int Sort { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    // Factory method
    public static User Create(string userName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));

        return new User(userName, email);
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
