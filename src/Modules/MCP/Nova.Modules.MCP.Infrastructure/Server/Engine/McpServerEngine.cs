using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// 处理一条 JSON-RPC 消息。
        /// <paramref name="mcpKey"/> 为本次请求使用的 MCP Key 明文，
        /// 用于「创建者关联」判定（公开工具 ∪ 该 Key 创建者自己的工具）。
        /// </summary>
        public async Task<bool> HandleMessageAsync(string sessionId, string? mcpKey, string jsonRpcMessage)
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

        /// <summary>
        /// 根据 MCP Key 明文解析其创建者（CreatedBy）。网关通道租户无关，
        /// 忽略多租户/软删除全局过滤器后按 KeyValue 命中。返回 null 表示无有效 Key。
        /// 「创建者关联」复用 McpKey 与 McpTool 上已有的 CreatedBy 审计字段，无需新增表/字段。
        /// </summary>
        private static async Task<Guid?> ResolveCreatorIdAsync(IMcpDbContext dbContext, string? mcpKey)
        {
            if (string.IsNullOrWhiteSpace(mcpKey))
            {
                return null;
            }

            var keyEntity = await dbContext.McpKeys
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.KeyValue == mcpKey);

            return keyEntity?.CreatedBy;
        }

        private async Task<JsonNode> HandleListToolsAsync(string? mcpKey)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMcpDbContext>();

            // 网关是公开的、租户无关的通道：第三方客户端只凭 MCP Key 访问，
            // 不会带租户上下文（也不应要求租户）。因此忽略多租户与软删除全局过滤器，
            // 仅按业务可见性筛选，使结果不依赖请求是否携带租户。
            // 可见工具 = （公开且启用）∪（当前 Key 创建者自己创建的工具，无论是否公开）。
            var creatorId = await ResolveCreatorIdAsync(dbContext, mcpKey);

            var publicTools = await dbContext.McpTools
                .IgnoreQueryFilters()
                .Where(t => t.IsEnabled && t.IsPublic && !t.IsDeleted)
                .ToListAsync();

            IQueryable<McpTool> ownQuery = dbContext.McpTools
                .IgnoreQueryFilters()
                .Where(t => t.IsEnabled && !t.IsDeleted);
            if (creatorId != null)
            {
                var cid = creatorId.Value;
                ownQuery = ownQuery.Where(t => t.CreatedBy == cid);
            }
            var ownTools = await ownQuery.ToListAsync();

            // 合并两份结果，按 McpTool.Id 去重（一个工具若既公开又属于当前创建者，只保留一条）。
            var tools = publicTools
                .Concat(ownTools)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
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

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMcpDbContext>();
            var executor = scope.ServiceProvider.GetRequiredService<HttpToolExecutor>();

            // 网关通道租户无关：忽略多租户/软删除过滤器，仅按业务可见性判定。
            var mcpTool = await dbContext.McpTools
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Name == toolName && t.IsEnabled && !t.IsDeleted);

            if (mcpTool == null)
            {
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Tool '{toolName}' not found or disabled." } } })!;
            }

            // 可见性校验：公开工具任意持有 Key 可调用；非公开工具仅限「创建者关联」的 Key 调用。
            var creatorId = await ResolveCreatorIdAsync(dbContext, mcpKey);
            if (!mcpTool.IsPublic && (creatorId == null || mcpTool.CreatedBy != creatorId))
            {
                return JsonSerializer.SerializeToNode(new { content = new[] { new { type = "text", text = $"Error: Tool '{toolName}' is private and not owned by this key." } } })!;
            }

            // FindAsync 本身绕过全局过滤器，这里再显式排除已软删除的服务端。
            var server = await dbContext.McpServers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == mcpTool.ServerId);
            if (server == null || server.IsDeleted)
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
