using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Modules.Mcp.Application.Common.Interfaces;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 更新 MCP 访问密钥名称。密钥值不可变更，需删除后重建。
/// </summary>
public class UpdateMcpKeyCommandHandler : IConsumer<UpdateMcpKeyCommand>
{
    private readonly IMcpDbContext _dbContext;

    public UpdateMcpKeyCommandHandler(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UpdateMcpKeyCommand> context)
    {
        var message = context.Message;

        var key = await _dbContext.McpKeys
            .FirstOrDefaultAsync(k => k.Id == message.Id, context.CancellationToken);

        if (key is null)
        {
            throw new NovaValidationException($"MCP 密钥 {message.Id} 不存在");
        }

        key.Rename(message.Name);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new UpdateMcpKeyResult { Id = key.Id });
    }
}
