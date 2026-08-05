using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Audit.Application.Database;
using Nova.Modules.Audit.Domain.OperationLogs;

namespace Nova.Modules.Audit.Infrastructure.Persistence;

public class AuditDbContext : DbContext, IAuditDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public DbSet<OperationLog> OperationLogs { get; set; } = default!;
    public DbSet<SanitizationDetail> SanitizationDetails { get; set; } = default!;

    public AuditDbContext(ITenantInfo tenantInfo, DbContextOptions<AuditDbContext> options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("audit");

        modelBuilder.Entity<OperationLog>(b =>
        {
            b.ToTable("OperationLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.TraceId).HasMaxLength(128).IsRequired(false);
            b.Property(x => x.ClientIp).HasMaxLength(64).IsRequired(false);
            b.Property(x => x.HttpMethod).HasMaxLength(16).IsRequired(false);
            b.Property(x => x.RequestPath).HasMaxLength(512).IsRequired(false);
            b.Property(x => x.ActionName).HasMaxLength(256).IsRequired(false);
            b.Property(x => x.ErrorMessage).HasMaxLength(2000).IsRequired(false);

            b.HasIndex(x => x.TraceId);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.UserId);

            b.HasMany(x => x.SanitizationDetails)
                .WithOne()
                .HasForeignKey(x => x.LogId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            b.IsMultiTenant();
        });

        modelBuilder.Entity<SanitizationDetail>(b =>
        {
            b.ToTable("SanitizationDetails");
            b.HasKey(x => x.Id);
            b.Property(x => x.FieldName).HasMaxLength(128).IsRequired(false);
            b.Property(x => x.MaskedRule).HasMaxLength(128).IsRequired(false);

            b.HasIndex(x => x.LogId);
            b.IsMultiTenant();
        });

        modelBuilder.ApplyTenantIsolationByDefault();
    }
}
