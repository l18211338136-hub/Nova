using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Storage.Application.Database;
using Nova.Modules.Storage.Domain.Attachments;
using Nova.Modules.Storage.Domain.Files;

namespace Nova.Modules.Storage.Infrastructure.Persistence;

public class StorageDbContext : DbContext, IStorageDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public DbSet<FileObject> FileObjects { get; set; } = default!;
    public DbSet<Attachment> Attachments { get; set; } = default!;

    public StorageDbContext(ITenantInfo tenantInfo, DbContextOptions<StorageDbContext> options)
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
        modelBuilder.HasDefaultSchema("storage");

        modelBuilder.Entity<FileObject>(b =>
        {
            b.ToTable("FileObjects");
            b.HasKey(x => x.Id);
            b.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            b.Property(x => x.FileKey).HasMaxLength(512).IsRequired();
            b.Property(x => x.BucketName).HasMaxLength(128).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            b.Property(x => x.FileHash).HasMaxLength(128).IsRequired(false);
            b.Property(x => x.AccessUrl).HasMaxLength(1024).IsRequired(false);

            b.HasIndex(x => x.FileKey);
            b.HasIndex(x => x.FileHash);
            b.HasIndex(x => x.CreatedAt);

            b.IsMultiTenant();
        });

        modelBuilder.Entity<Attachment>(b =>
        {
            b.ToTable("Attachments");
            b.HasKey(x => x.Id);
            b.Property(x => x.TargetType).HasMaxLength(128).IsRequired();
            b.Property(x => x.TargetId).HasMaxLength(128).IsRequired();
            b.Property(x => x.Remarks).HasMaxLength(256).IsRequired(false);

            b.HasIndex(x => x.FileId);
            b.HasIndex(x => new { x.TargetType, x.TargetId });
            b.HasIndex(x => x.AttachmentType);

            b.IsMultiTenant();
        });

        modelBuilder.ApplyTenantIsolationByDefault();
    }
}
