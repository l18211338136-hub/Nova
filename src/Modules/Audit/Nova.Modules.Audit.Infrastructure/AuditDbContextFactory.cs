using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Audit.Infrastructure.Persistence;

namespace Nova.Modules.Audit.Infrastructure;

public class AuditDbContextFactory : DesignTimeDbContextFactoryBase<AuditDbContext>
{
    protected override AuditDbContext CreateDbContext(DbContextOptions<AuditDbContext> options)
    {
        var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
        return new AuditDbContext(dummyTenant, options);
    }
}
