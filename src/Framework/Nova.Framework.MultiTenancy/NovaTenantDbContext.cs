using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;

namespace Nova.Framework.MultiTenancy;

public class NovaTenantDbContext : EFCoreStoreDbContext<NovaTenantInfo>
{
    public NovaTenantDbContext(DbContextOptions<NovaTenantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("system");
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyTenantIsolationByDefault();
    }
}
