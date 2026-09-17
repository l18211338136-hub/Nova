using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Modules.Mcp.Application.Common.Interfaces;

namespace Nova.Modules.Mcp.Application.Tools.Commands;

/// <summary>
/// 删除 MCP 工具（软删除，可由「数据回收」页面恢复）。
/// </summary>
public class DeleteMcpToolCommandHandler : IConsumer<DeleteMcpToolCommand>
{
    private readonly IMcpDbContext _dbContext;

    public DeleteMcpToolCommandHandler(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<DeleteMcpToolCommand> context)
    {
        var id = context.Message.Id;

        var tool = await _dbContext.McpTools
            .FirstOrDefaultAsync(t => t.Id == id, context.CancellationToken);

        if (tool is null)
        {
            throw new NovaValidationException($"MCP 工具 {id} 不存在");
        }

        _dbContext.McpTools.Remove(tool);

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new DeleteMcpToolResult { Success = true });
    }
}
