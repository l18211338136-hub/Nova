using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Modules.Mcp.Application.Common.Interfaces;

namespace Nova.Modules.Mcp.Application.Servers.Commands;

/// <summary>
/// 删除 MCP 服务。
/// <para>
/// 领域不变量：<c>McpTool</c> 是 <c>McpServer</c> 聚合内的子实体，服务被删除后工具不应继续存在，
/// 因此在同一个工作单元内级联删除其下所有工具，保证聚合一致性。
/// </para>
/// <para>
/// 注意：框架的软删除由 <c>AuditableEntitySaveChangesInterceptor</c> 在实体进入
/// <see cref="EntityState.Deleted"/> 时改写为 <c>IsDeleted = true</c>，数据库层的
/// <c>ON DELETE CASCADE</c> 永远不会触发，所以级联必须在这里显式处理。
/// </para>
/// </summary>
public class DeleteMcpServerCommandHandler : IConsumer<DeleteMcpServerCommand>
{
    private readonly IMcpDbContext _dbContext;

    public DeleteMcpServerCommandHandler(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<DeleteMcpServerCommand> context)
    {
        var id = context.Message.Id;

        var server = await _dbContext.McpServers
            .FirstOrDefaultAsync(s => s.Id == id, context.CancellationToken);

        if (server is null)
        {
            throw new NovaValidationException($"MCP 服务 {id} 不存在");
        }

        // 聚合一致性：先级联软删除子实体，再删除聚合根
        var tools = await _dbContext.McpTools
            .Where(t => t.ServerId == id)
            .ToListAsync(context.CancellationToken);

        if (tools.Count > 0)
        {
            _dbContext.McpTools.RemoveRange(tools);
        }

        _dbContext.McpServers.Remove(server);

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new DeleteMcpServerResult
        {
            Success = true,
            DeletedToolCount = tools.Count
        });
    }
}
