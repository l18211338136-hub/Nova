using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;

namespace Nova.Modules.Notification.Infrastructure;

public class NotificationDbContextFactory : DesignTimeDbContextFactoryBase<NotificationDbContext>
{
    protected override NotificationDbContext CreateDbContext(DbContextOptions<NotificationDbContext> options)
    {
        var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
        return new NotificationDbContext(dummyTenant, options);
    }
}
