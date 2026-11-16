using MassTransit;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Domain.Entities;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 创建 MCP 访问密钥（聚合根式实体，密钥值由领域工厂内部生成）。
/// </summary>
public class CreateMcpKeyCommandHandler : IConsumer<CreateMcpKeyCommand>
{
    private readonly IMcpDbContext _dbContext;

    public CreateMcpKeyCommandHandler(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<CreateMcpKeyCommand> context)
    {
        var message = context.Message;
        var key = McpKey.Create(message.Name, message.AllServers);

        var accesses = await McpKeyScopeHelper.BuildAndValidateAccessesAsync(
            _dbContext,
            key.Id,
            message.Name,
            message.AllServers,
            message.Scopes,
            context.CancellationToken);

        _dbContext.McpKeys.Add(key);
        if (accesses.Count > 0)
        {
            _dbContext.McpKeyAccesses.AddRange(accesses);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new CreateMcpKeyResult
        {
            Id = key.Id,
            KeyValue = key.KeyValue
        });
    }
}
