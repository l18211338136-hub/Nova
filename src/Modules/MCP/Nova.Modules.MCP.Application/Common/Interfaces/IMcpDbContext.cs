using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nova.Modules.Mcp.Domain.Entities;

namespace Nova.Modules.Mcp.Application.Common.Interfaces;

public interface IMcpDbContext
{
    DbSet<McpServer> McpServers { get; }
    DbSet<McpTool> McpTools { get; }
    DbSet<McpKey> McpKeys { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
