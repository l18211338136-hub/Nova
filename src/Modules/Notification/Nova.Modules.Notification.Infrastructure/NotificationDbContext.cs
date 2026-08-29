using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Notification;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Extensions;
using Nova.Modules.Notification.Domain;

namespace Nova.Modules.Notification.Infrastructure;

public interface INotificationDbContext
{
    DbSet<SystemNotification> SystemNotifications { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class NotificationDbContext : DbContext, INotificationDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public NotificationDbContext(ITenantInfo tenantInfo, DbContextOptions<NotificationDbContext> options) 
        : base(options)
    {
        TenantInfo = tenantInfo;
    }

    public DbSet<SystemNotification> SystemNotifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<SystemNotification>(b =>
        {
            b.ToTable("SystemNotifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(256);
            b.Property(x => x.EventCode).HasMaxLength(100);
            b.Property(x => x.NotificationType)
             .HasConversion(v => v.ToString(), v => (NotificationType)Enum.Parse(typeof(NotificationType), v))
             .HasMaxLength(50);
            b.Property(x => x.ReferenceId).HasMaxLength(100);

            b.IsMultiTenant();
        });

        // 统一应用软删除过滤器
        modelBuilder.ApplySoftDeleteQueryFilter();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
