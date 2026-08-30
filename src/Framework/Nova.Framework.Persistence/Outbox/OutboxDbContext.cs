using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Nova.Framework.Persistence.Outbox;

public class OutboxDbContext : DbContext, IMultiTenantDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<InboxMessage> InboxMessages { get; set; } = null!;

    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public OutboxDbContext(DbContextOptions<OutboxDbContext> options, ITenantInfo tenantInfo) 
        : base(options)
    {
        TenantInfo = tenantInfo;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OutboxMessage>(b =>
        {
            // 放到系统 schema 中，也可以不设置
            b.ToTable("OutboxMessages");
            b.HasKey(m => m.Id);
            b.Property(m => m.EventType).IsRequired().HasMaxLength(256);
            b.Property(m => m.PayloadJson).IsRequired().HasColumnType("jsonb");
            b.HasIndex(m => m.ProcessedAt);
            b.IsMultiTenant();
        });

        builder.Entity<InboxMessage>(b =>
        {
            b.ToTable("InboxMessages");
            b.HasKey(m => new { m.Id, m.ConsumerName });
            b.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
            b.IsMultiTenant();
        });
    }
}
