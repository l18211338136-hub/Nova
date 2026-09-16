using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Mcp.Infrastructure.Persistence;

namespace Nova.Modules.MCP.Infrastructure.Persistence
{
    public class McpDbContextFactory : DesignTimeDbContextFactoryBase<McpDbContext>
    {
        protected override McpDbContext CreateDbContext(DbContextOptions<McpDbContext> options)
        {
            var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
            return new McpDbContext(dummyTenant, options);
        }
    }
}
