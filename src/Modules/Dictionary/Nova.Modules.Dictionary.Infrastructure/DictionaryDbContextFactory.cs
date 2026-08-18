using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Dictionary.Infrastructure.Persistence;

namespace Nova.Modules.Dictionary.Infrastructure;

public class DictionaryDbContextFactory : DesignTimeDbContextFactoryBase<DictionaryDbContext>
{
    protected override DictionaryDbContext CreateDbContext(DbContextOptions<DictionaryDbContext> options)
    {
        var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
        return new DictionaryDbContext(dummyTenant, options);
    }
}
