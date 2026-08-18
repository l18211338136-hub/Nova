using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Extensions;
using Nova.Modules.Dictionary.Application.Database;
using Nova.Modules.Dictionary.Domain.DictionaryItems;
using Nova.Modules.Dictionary.Domain.DictionaryTypes;

namespace Nova.Modules.Dictionary.Infrastructure.Persistence;

public class DictionaryDbContext : DbContext, IDictionaryDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public DbSet<DictionaryType> DictionaryTypes { get; set; } = default!;
    public DbSet<DictionaryItem> DictionaryItems { get; set; } = default!;

    public DictionaryDbContext(ITenantInfo tenantInfo, DbContextOptions<DictionaryDbContext> options)
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
        modelBuilder.HasDefaultSchema("dictionary");

        modelBuilder.Entity<DictionaryType>(b =>
        {
            b.ToTable("DictionaryTypes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(128).IsRequired();
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Description).HasMaxLength(512).IsRequired(false);

            b.HasIndex(x => x.Code);
            b.HasIndex(x => x.Sort);
            b.IsMultiTenant();
        });

        modelBuilder.Entity<DictionaryItem>(b =>
        {
            b.ToTable("DictionaryItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.TypeCode).HasMaxLength(128).IsRequired();
            b.Property(x => x.Label).HasMaxLength(128).IsRequired();
            b.Property(x => x.Value).HasMaxLength(256).IsRequired();
            b.Property(x => x.TagType).HasMaxLength(64).IsRequired(false);
            b.Property(x => x.Remarks).HasMaxLength(512).IsRequired(false);

            b.HasIndex(x => x.TypeId);
            b.HasIndex(x => x.TypeCode);
            b.HasIndex(x => x.Sort);
            b.IsMultiTenant();
        });

        modelBuilder.ApplyTenantIsolationByDefault();
        modelBuilder.ApplySoftDeleteQueryFilter();
    }
}
