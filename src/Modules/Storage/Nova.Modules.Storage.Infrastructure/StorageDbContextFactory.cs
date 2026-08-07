using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Storage.Infrastructure.Persistence;

namespace Nova.Modules.Storage.Infrastructure;

public class StorageDbContextFactory : DesignTimeDbContextFactoryBase<StorageDbContext>
{
    protected override StorageDbContext CreateDbContext(DbContextOptions<StorageDbContext> options)
    {
        var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
        return new StorageDbContext(dummyTenant, options);
    }
}
