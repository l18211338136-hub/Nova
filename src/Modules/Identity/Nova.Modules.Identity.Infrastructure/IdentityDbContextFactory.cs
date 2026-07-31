using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;

namespace Nova.Modules.Identity.Infrastructure;

public class IdentityDbContextFactory : DesignTimeDbContextFactoryBase<IdentityDbContext>
{
    protected override IdentityDbContext CreateDbContext(DbContextOptions<IdentityDbContext> options)
    {
        var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
        return new IdentityDbContext(dummyTenant, options);
    }
}
