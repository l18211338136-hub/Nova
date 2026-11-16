using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Domain.Entities;
using Nova.Modules.Mcp.Domain.ValueObjects;
using Nova.Modules.Mcp.Infrastructure.Providers.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Nova.Modules.Mcp.Infrastructure.Server.Engine
{
    public class McpServerEngine : IMcpServerEngine
    {
        private readonly ConcurrentDictionary<string, Channel<string>> _activeSessions = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<McpServerEngine> _logger;

        public McpServerEngine(IServiceScopeFactory scopeFactory, ILogger<McpServerEngine> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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

        /// <summary>
        /// 处理一条 JSON-RPC 消息。
        /// <paramref name="mcpKey"/> 为本次请求使用的 MCP Key 明文，
        /// 用于「创建者关联」判定（公开工具 ∪ 该 Key 创建者自己的工具）。
        /// </summary>
        public async Task<bool> HandleMessageAsync(string sessionId, string? mcpKey, string jsonRpcMessage)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var channel))
            {
                _logger.LogWarning("[MCP Engine] SessionId not found: {SessionId}", sessionId);
                return false;
            }

            string? id = null;
            try
            {
                var requestNode = JsonNode.Parse(jsonRpcMessage);
                if (requestNode == null) return true;

                id = requestNode["id"]?.ToString();
                string? method = requestNode["method"]?.ToString();

                _logger.LogInformation("[MCP Engine] HandleMessage => Session: {SessionId}, Method: {Method}, Id: {Id}, Key: {Key}",
                    sessionId, method, id, string.IsNullOrEmpty(mcpKey) ? "(none)" : $"{mcpKey[..Math.Min(12, mcpKey.Length)]}...");

                JsonNode? resultData = null;
                switch (method)
                {
                    case "initialize":
                        resultData = HandleInitialize();
                        break;
                    case "tools/list":
                        resultData = await HandleListToolsAsync(mcpKey);
                        break;
                    case "tools/call":
                        resultData = await HandleCallToolAsync(requestNode["params"], mcpKey);
                        break;
                    default:
                        _logger.LogWarning("[MCP Engine] Unhandled method: {Method}", method);
                        return true;
                }

                var responseObj = new
                {
                    jsonrpc = "2.0",
                    id = id,
                    result = resultData
                };

                string responseStr = JsonSerializer.Serialize(responseObj);
                _logger.LogInformation("[MCP Engine] Pushing response for Id: {Id}, Length: {Length}", id, responseStr.Length);
                await channel.Writer.WriteAsync(responseStr);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MCP Engine] ERROR while handling message for Session: {SessionId}, Id: {Id}: {Message}",
                    sessionId, id, ex.Message);

                // 发送 JSON-RPC 错误响应给客户端，避免客户端死等超时
                if (!string.IsNullOrEmpty(id))
                {
                    var errorObj = new
                    {
                        jsonrpc = "2.0",
                        id = id,
                        error = new
                        {
                            code = -32603,
                            message = $"Internal error: {ex.Message}"
                        }
                    };
                    await channel.Writer.WriteAsync(JsonSerializer.Serialize(errorObj));
                }
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

        private sealed record KeyAccessContext(McpKey Key, IReadOnlyList<McpKeyAccess> Accesses);

        /// <summary>
        /// 根据 MCP Key 明文解析密钥及授权范围明细（忽略多租户/软删除过滤器，
        /// 仅匹配仍处于有效期的密钥）。
        /// </summary>
        private static async Task<KeyAccessContext?> ResolveKeyAccessAsync(IMcpDbContext dbContext, string? mcpKey)
        {
            if (string.IsNullOrWhiteSpace(mcpKey))
            {
                return null;
            }

            var keyEntity = await dbContext.McpKeys
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KeyValue == mcpKey);

            if (keyEntity is null || !keyEntity.IsActive(DateTimeOffset.UtcNow))
            {
                return null;
            }

            if (keyEntity.AllServers)
            {
                return new KeyAccessContext(keyEntity, Array.Empty<McpKeyAccess>());
            }

            var accesses = await dbContext.McpKeyAccesses
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.McpKeyId == keyEntity.Id && !a.IsDeleted)
                .ToListAsync();

            return new KeyAccessContext(keyEntity, accesses);
        }

        private static bool IsToolAuthorized(McpTool tool, KeyAccessContext? accessContext)
        {
            if (accessContext is null) return false;
            if (accessContext.Key.AllServers) return true;

            return accessContext.Accesses.Any(a =>
                a.ServerId == tool.ServerId &&
                (a.ToolId is null || a.ToolId == tool.Id));
        }

        private async Task<JsonNode> HandleListToolsAsync(string? mcpKey)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMcpDbContext>();

            var accessContext = await ResolveKeyAccessAsync(dbContext, mcpKey);
            if (accessContext is null)
            {
                return JsonSerializer.SerializeToNode(new { tools = Array.Empty<object>() })!;
            }

            var enabledTools = await dbContext.McpTools
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.IsEnabled && !t.IsDeleted)
                .ToListAsync();

            var tools = enabledTools
                .Where(t => IsToolAuthorized(t, accessContext))
                .ToList();

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

        private async Task<JsonNode> HandleCallToolAsync(JsonNode? paramNode, string? mcpKey)
        {
            if (paramNode == null) return null;

            string? toolName = paramNode["name"]?.ToString();
            var arguments = paramNode["arguments"];

            _logger.LogInformation("[MCP Engine] HandleCallToolAsync => ToolName: {ToolName}, Arguments: {Arguments}",
                toolName, arguments?.ToJsonString() ?? "(null)");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMcpDbContext>();
            var executor = scope.ServiceProvider.GetRequiredService<HttpToolExecutor>();

            // 网关通道租户无关：忽略多租户/软删除过滤器，仅按业务可见性判定。
            var mcpTool = await dbContext.McpTools
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Name == toolName && t.IsEnabled && !t.IsDeleted);

            if (mcpTool == null)
            {
                _logger.LogWarning("[MCP Engine] McpTool '{ToolName}' NOT found or disabled/deleted in database.", toolName);
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Tool '{toolName}' not found or disabled." } } })!;
            }

            _logger.LogInformation(
                "[MCP Engine] McpTool Entity => Id: {ToolId}, Name: {Name}, ServerId: {ServerId}, RoutePath: {RoutePath}, Method: {Method}, IsPublic: {IsPublic}, CreatedBy: {CreatedBy}",
                mcpTool.Id, mcpTool.Name, mcpTool.ServerId, mcpTool.RoutePath, mcpTool.HttpMethod, mcpTool.IsPublic, mcpTool.CreatedBy);

            // 授权范围校验：必须与 HandleListToolsAsync 保持一致，防止知道工具名直接越权调用
            var accessContext = await ResolveKeyAccessAsync(dbContext, mcpKey);
            if (!IsToolAuthorized(mcpTool, accessContext))
            {
                _logger.LogWarning("[MCP Engine] Key is not authorized to call tool '{ToolName}' (ToolId: {ToolId}, ServerId: {ServerId}).",
                    toolName, mcpTool.Id, mcpTool.ServerId);
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Tool '{toolName}' is not authorized for this key." } } })!;
            }

            // FindAsync 本身绕过全局过滤器，这里再显式排除已软删除的服务端。
            var server = await dbContext.McpServers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == mcpTool.ServerId);
            if (server == null || server.IsDeleted)
            {
                _logger.LogWarning("[MCP Engine] McpServer (Id: {ServerId}) for Tool '{ToolName}' NOT found or deleted.", mcpTool.ServerId, toolName);
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Server for tool '{toolName}' not found." } } })!;
            }

            _logger.LogInformation(
                "[MCP Engine] McpServer Entity => Id: {ServerId}, Name: {ServerName}, BaseUrl: {BaseUrl}, HasAuthToken: {HasAuthToken}",
                server.Id, server.Name, server.BaseUrl, !string.IsNullOrEmpty(server.AuthToken));

            var profile = new ExecutionProfile
            {
                TargetUrl = $"{server.BaseUrl.TrimEnd('/')}/{(mcpTool.RoutePath ?? string.Empty).TrimStart('/')}",
                TargetMethod = mcpTool.HttpMethod,
                AuthToken = server.AuthToken,
                ParameterMap = JsonSerializer.Deserialize<Dictionary<string, ParameterMapping>>(mcpTool.ParameterMap) ?? new()
            };

            _logger.LogInformation(
                "[MCP Engine] Ready to execute => TargetUrl: {Method} {TargetUrl}",
                profile.TargetMethod, profile.TargetUrl);

            string rawResult = await executor.ExecuteAsync(profile, arguments!);

            _logger.LogInformation(
                "[MCP Engine] Execute Finished => Tool: {ToolName}, RawResult Length: {Length}",
                toolName, rawResult?.Length ?? 0);

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
