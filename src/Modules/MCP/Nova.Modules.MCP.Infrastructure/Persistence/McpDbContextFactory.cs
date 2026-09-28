using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Mcp.Infrastructure.Persistence;

namespace Nova.Modules.MCP.Infrastructure.Persistence
{
    public class McpDbContextFactory : DesignTimeDbContextFactoryBase<McpDbContext>
    {
        /// <summary>
        /// MCP 使用独立数据库（McpConnection / nova_mcp_db），设计期迁移与启动自动迁移都落到该库，
        /// 与 Identity/Audit 等模块的主库（DefaultConnection）物理隔离。
        /// 所有租户共享同一物理库，靠实体上的 TenantId 列做逻辑隔离。
        /// </summary>
        protected override string ConnectionStringName => "McpConnection";

        protected override McpDbContext CreateDbContext(DbContextOptions<McpDbContext> options)
        {
            var dummyTenant = new NovaTenantInfo { Id = "dummy", Identifier = "dummy" };
            return new McpDbContext(dummyTenant, options);
        }
    }
}
