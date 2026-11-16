using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.Dtos;

namespace Nova.Modules.Mcp.Application.Servers.Commands;

/// <summary>
/// 更新 MCP 服务基本信息。
/// 修改 BaseUrl 或 SwaggerUrl 不会自动重建其下工具。
/// </summary>
public class UpdateMcpServerCommandHandler : IConsumer<UpdateMcpServerCommand>
{
    private readonly IMcpDbContext _dbContext;

    public UpdateMcpServerCommandHandler(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UpdateMcpServerCommand> context)
    {
        var message = context.Message;

        var server = await _dbContext.McpServers
            .FirstOrDefaultAsync(s => s.Id == message.Id, context.CancellationToken);

        if (server is null)
        {
            throw new NovaValidationException($"MCP 服务 {message.Id} 不存在");
        }

        server.Update(message.Name, message.BaseUrl, message.SwaggerUrl, message.AuthToken);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new McpServerDto
        {
            Id = server.Id,
            Name = server.Name,
            BaseUrl = server.BaseUrl,
            SwaggerUrl = server.SwaggerUrl,
            CreatedAt = server.CreatedAt
        });
    }
}
