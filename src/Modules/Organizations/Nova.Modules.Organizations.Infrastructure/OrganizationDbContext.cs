using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Extensions;
using Nova.Modules.Organizations.Application.Database;
using Nova.Modules.Organizations.Domain;

namespace Nova.Modules.Organizations.Infrastructure;

public class OrganizationDbContext : DbContext, IOrganizationDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<UserOrganization> UserOrganizations { get; set; }

    public OrganizationDbContext(ITenantInfo tenantInfo, DbContextOptions<OrganizationDbContext> options)
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

        builder.ApplyTenantIsolationByDefault();
        builder.HasDefaultSchema("organization");

        builder.Entity<Organization>(b =>
        {
            b.ToTable("Organizations");
            b.HasKey(o => o.Id);
            b.Property(o => o.Name).IsRequired().HasMaxLength(100);
            b.Property(o => o.Code).HasMaxLength(50);
            b.Property(o => o.Type).HasMaxLength(50);
            b.Property(o => o.Phone).HasMaxLength(30);
            b.Property(o => o.Email).HasMaxLength(100);
            b.Property(o => o.Remarks).HasMaxLength(500);

            // 自引用树状结构外键约束
            b.HasOne<Organization>()
             .WithMany()
             .HasForeignKey(o => o.ParentId)
             .OnDelete(DeleteBehavior.Restrict);

            b.IsMultiTenant();
        });

        builder.Entity<UserOrganization>(b =>
        {
            b.ToTable("UserOrganizations");
            b.HasKey(uo => uo.Id);
            b.Property(uo => uo.JobTitle).HasMaxLength(50);
            b.HasIndex(uo => uo.UserId);
            b.HasIndex(uo => uo.OrganizationId);

            b.IsMultiTenant();
        });

        builder.ApplySoftDeleteQueryFilter();
    }
}
