using Microsoft.EntityFrameworkCore;

namespace Nova.Framework.MultiTenancy.EntityFrameworkCore;

public class NovaTenantDbContextFactory : DesignTimeDbContextFactoryBase<NovaTenantDbContext>
{
    protected override NovaTenantDbContext CreateDbContext(DbContextOptions<NovaTenantDbContext> options)
    {
        return new NovaTenantDbContext(options);
    }
}
