using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;

namespace Nova.Modules.Organizations.Infrastructure;

public class OrganizationDbContextFactory : DesignTimeDbContextFactoryBase<OrganizationDbContext>
{
    protected override OrganizationDbContext CreateDbContext(DbContextOptions<OrganizationDbContext> options)
    {
        var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
        return new OrganizationDbContext(dummyTenant, options);
    }
}
