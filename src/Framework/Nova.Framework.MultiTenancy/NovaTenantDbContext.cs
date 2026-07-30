using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;

namespace Nova.Framework.MultiTenancy;

public class NovaTenantDbContext : EFCoreStoreDbContext<NovaTenantInfo>
{
    public DbSet<GlobalUserTenantMapping> GlobalUserTenantMappings { get; set; }

    public NovaTenantDbContext(DbContextOptions<NovaTenantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("system");
        base.OnModelCreating(modelBuilder);
        
        // 此表不需要租户隔离，它是跨租户的全局表
        modelBuilder.Entity<GlobalUserTenantMapping>(b =>
        {
            b.HasIndex(x => x.Account);
            b.HasIndex(x => new { x.Account, x.TenantId }).IsUnique();
        });

        modelBuilder.ApplyTenantIsolationByDefault();
    }
}
