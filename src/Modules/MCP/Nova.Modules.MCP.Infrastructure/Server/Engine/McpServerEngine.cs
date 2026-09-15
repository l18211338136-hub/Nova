using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Infrastructure.Providers.Http;
using Nova.Modules.Mcp.Domain.ValueObjects;
using System.Linq;
using System.Collections.Generic;

namespace Nova.Modules.Mcp.Infrastructure.Server.Engine
{
    public class McpServerEngine : IMcpServerEngine
    {
        private readonly ConcurrentDictionary<string, Channel<string>> _activeSessions = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public McpServerEngine(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task HoldSseConnectionAsync(string sessionId, HttpResponse response, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();
            _activeSessions.TryAdd(sessionId, channel);

            try
            {
                await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await response.WriteAsync($"event: message\ndata: {message}\n\n", cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _activeSessions.TryRemove(sessionId, out _);
            }
        }

        public async Task<bool> HandleMessageAsync(string sessionId, string jsonRpcMessage)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var channel))
            {
                return false;
            }

            try
            {
                var requestNode = JsonNode.Parse(jsonRpcMessage);
                if (requestNode == null) return true;

                string? id = requestNode["id"]?.ToString();
                string? method = requestNode["method"]?.ToString();
                
                JsonNode resultData = null!;
                switch (method)
                {
                    case "initialize":
                        resultData = HandleInitialize();
                        break;
                    case "tools/list":
                        resultData = await HandleListToolsAsync(sessionId);
                        break;
                    case "tools/call":
                        resultData = await HandleCallToolAsync(requestNode["params"]);
                        break;
                    default:
                        return true; 
                }

                var responseObj = new
                {
                    jsonrpc = "2.0",
                    id = id,
                    result = resultData
                };

                string responseStr = JsonSerializer.Serialize(responseObj);
                await channel.Writer.WriteAsync(responseStr);
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private JsonNode HandleInitialize()
        {
            var initResult = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "Nova-Mcp-Market", version = "1.0.0" }
            };
            return JsonSerializer.SerializeToNode(initResult)!;
        }

        private async Task<JsonNode> HandleListToolsAsync(string sessionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMcpDbContext>();

            var tools = await dbContext.McpTools
                .Where(t => t.IsEnabled && t.IsPublic)
                .ToListAsync();

            var toolList = tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = JsonNode.Parse(t.InputSchema)
            }).ToArray();

            var toolsResult = new
            {
                tools = toolList
            };
            return JsonSerializer.SerializeToNode(toolsResult)!;
        }

        private async Task<JsonNode> HandleCallToolAsync(JsonNode? paramNode)
        {
            if (paramNode == null) return null;
            
            string? toolName = paramNode["name"]?.ToString();
            var arguments = paramNode["arguments"];

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMcpDbContext>();
            var executor = scope.ServiceProvider.GetRequiredService<HttpToolExecutor>();

            var mcpTool = await dbContext.McpTools
                .FirstOrDefaultAsync(t => t.Name == toolName && t.IsEnabled);

            if (mcpTool == null) 
            {
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Tool '{toolName}' not found or disabled." } } })!;
            }

            var server = await dbContext.McpServers.FindAsync(mcpTool.ServerId);
            if (server == null)
            {
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Server for tool '{toolName}' not found." } } })!;
            }

            var profile = new ExecutionProfile
            {
                TargetUrl = server.BaseUrl.TrimEnd('/') + mcpTool.RoutePath,
                TargetMethod = mcpTool.HttpMethod,
                AuthToken = server.AuthToken,
                ParameterMap = JsonSerializer.Deserialize<Dictionary<string, ParameterMapping>>(mcpTool.ParameterMap) ?? new()
            };

            string rawResult = await executor.ExecuteAsync(profile, arguments!);

            var callResult = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = rawResult
                    }
                }
            };
            return JsonSerializer.SerializeToNode(callResult)!;
        }
    }
}
