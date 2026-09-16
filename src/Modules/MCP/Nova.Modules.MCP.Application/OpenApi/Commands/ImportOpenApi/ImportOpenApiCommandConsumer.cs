using MassTransit;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Domain.Entities;

namespace Nova.Modules.Mcp.Application.OpenApi.Commands.ImportOpenApi;

public class ImportOpenApiCommandConsumer : IConsumer<ImportOpenApiCommand>
{
    private readonly IMcpDbContext _dbContext;
    private readonly IOpenApiToolGenerator _generator;

    public ImportOpenApiCommandConsumer(IMcpDbContext dbContext, IOpenApiToolGenerator generator)
    {
        _dbContext = dbContext;
        _generator = generator;
    }

    public async Task Consume(ConsumeContext<ImportOpenApiCommand> context)
    {
        var request = context.Message;
        
        // 1. 创建并保存 Server 聚合根
        var server = McpServer.Create(
            name: request.ServerName,
            baseUrl: request.BaseUrl,
            swaggerUrl: request.SwaggerUrl,
            authToken: request.AuthToken);

        _dbContext.McpServers.Add(server);

        // 2. 使用解析器将 Swagger 翻译为 MCP 标准格式
        var tools = _generator.GenerateToolsFromSwagger(request.SwaggerJson, request.BaseUrl);

        // 2.1 若指定了 SelectedOperations（"METHOD 路径"），则仅保留用户勾选的接口
        if (request.SelectedOperations is { Count: > 0 })
        {
            var basePath = request.BaseUrl.TrimEnd('/');
            var selected = request.SelectedOperations
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim().ToUpperInvariant())
                .ToHashSet();

            tools = tools
                .Where(t =>
                {
                    var path = t.Profile.TargetUrl.StartsWith(basePath, StringComparison.Ordinal)
                        ? t.Profile.TargetUrl[basePath.Length..]
                        : t.Profile.TargetUrl;
                    return selected.Contains($"{t.Profile.TargetMethod} {path}".ToUpperInvariant());
                })
                .ToList();
        }

        // 3. 将解析出的每一个接口包装为 McpTool 落库
        foreach (var toolDef in tools)
        {
            var mcpTool = McpTool.Create(
                serverId: server.Id,
                name: toolDef.Name,
                description: toolDef.Description,
                httpMethod: toolDef.Profile.TargetMethod,
                routePath: toolDef.Profile.TargetUrl.Replace(request.BaseUrl, ""),
                inputSchema: JsonSerializer.Serialize(toolDef.InputSchema),
                parameterMap: JsonSerializer.Serialize(toolDef.Profile.ParameterMap),
                isPublic: true,
                isEnabled: true
            );

            _dbContext.McpTools.Add(mcpTool);
        }

        // 4. 一次性提交事务
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        // 5. 通过 MassTransit 的 Request/Response 模式回写响应
        if (context.RequestId != null)
        {
            await context.RespondAsync(new ImportOpenApiCommandResponse { ServerId = server.Id });
        }
    }
}
