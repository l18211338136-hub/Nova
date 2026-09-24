using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Modules.Mcp.Application.Common.Interfaces;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 删除 MCP 访问密钥。
/// 软删除由 AuditableEntitySaveChangesInterceptor 在实体进入 Deleted 状态时改写为 IsDeleted = true，
/// 因此这里直接 Remove 即可，数据库层 ON DELETE 不会触发。
/// </summary>
public class DeleteMcpKeyCommandHandler : IConsumer<DeleteMcpKeyCommand>
{
    private readonly IMcpDbContext _dbContext;

    public DeleteMcpKeyCommandHandler(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<DeleteMcpKeyCommand> context)
    {
        var id = context.Message.Id;

        var key = await _dbContext.McpKeys
            .FirstOrDefaultAsync(k => k.Id == id, context.CancellationToken);

        if (key is null)
        {
            throw new NovaValidationException($"MCP 密钥 {id} 不存在");
        }

        _dbContext.McpKeys.Remove(key);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new DeleteMcpKeyResult { Success = true });
    }
}
