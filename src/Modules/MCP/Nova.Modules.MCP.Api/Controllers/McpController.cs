using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Nova.Framework.Web.Controllers;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ImportOpenApi;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ParseSwagger;
using Nova.Modules.Mcp.Application.Dtos;
using Nova.Modules.Mcp.Infrastructure.Persistence;
using Mapster;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.Web.Responses;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Api.Controllers
{
    /// <summary>
    /// MCP 协议标准网关控制器
    /// 处理第三方 AI 客户端的 SSE 连接与 JSON-RPC 消息分发
    /// </summary>
    [ApiController]
    [Route("api/mcp")]
    // [Authorize] // TODO: 视情况加上你们现有的鉴权标签，校验第三方 API Key
    public class McpController : NovaControllerBase
    {
        private readonly IMcpServerEngine _mcpEngine;
        
        public McpController(IMcpServerEngine mcpEngine)
        {
            _mcpEngine = mcpEngine;
        }

        /// <summary>
        /// 1. 建立 SSE (Server-Sent Events) 长连接
        /// 第三方大模型 (如 Claude Desktop) 首先会调用此接口
        /// </summary>
        [HttpGet("sse")]
        [EndpointSummary("建立连接")]
        public async Task GetSseConnection()
        {
            // 必须设置正确的 SSE 响应头
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // 生成一个全局唯一的 SessionId
            string sessionId = System.Guid.NewGuid().ToString();
            
            // 建立连接后推送 endpoint 事件，告知客户端后续发送消息的地址
            string? postEndpoint = Url.Action(nameof(PostMessage), "Mcp", new { sessionId }, Request.Scheme);
            
            string initEvent = $"event: endpoint\ndata: {postEndpoint}\n\n";
            await Response.WriteAsync(initEvent);
            await Response.Body.FlushAsync();

            // 保持长连接不断开，交由底层的 Session 管理器接管
            await _mcpEngine.HoldSseConnectionAsync(sessionId, Response, HttpContext.RequestAborted);
        }

        /// <summary>
        /// 2. 接收大模型的 JSON-RPC 消息
        /// </summary>
        [HttpPost("messages")]
        [EndpointSummary("接收消息")]
        public async Task<IActionResult> PostMessage([FromQuery] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Missing sessionId.");
            }

            using var reader = new StreamReader(Request.Body);
            string jsonRpcMessage = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonRpcMessage))
            {
                return BadRequest("Empty message.");
            }

            // 将收到的消息扔给底层的 MCP 引擎，引擎处理完后自动通过 SSE 回推
            bool success = await _mcpEngine.HandleMessageAsync(sessionId, jsonRpcMessage);
            if (!success)
            {
                return NotFound("Session not found or expired.");
            }

            return Accepted();
        }

        /// <summary>
        /// 3. 从前端管理员面板导入 Swagger JSON
        /// 负责将现有的 Web API 自动转化为 MCP 代理工具
        /// </summary>
        [HttpPost("import")]
        [EndpointSummary("导入配置")]
        [Authorize]
        [RequirePermission("Mcp.Servers.Import")]
        public async Task<IActionResult> ImportOpenApi([FromBody] ImportOpenApiCommand command)
        {
            var response = await SendRequestAsync<ImportOpenApiCommand, ImportOpenApiCommandResponse>(command);
            return Ok(new { success = true, serverId = response.ServerId });
        }

        /// <summary>
        /// 3.1 解析 Swagger（地址 / JSON 二选一）
        /// 返回最终用于解析的 Swagger JSON 与全部接口操作，供前端回填与勾选
        /// </summary>
        [HttpPost("parse")]
        [EndpointSummary("解析接口")]
        [Authorize]
        [RequirePermission("Mcp.Servers.Import")]
        public async Task<IActionResult> ParseSwagger([FromBody] ParseSwaggerCommand command)
        {
            var response = await SendRequestAsync<ParseSwaggerCommand, ParseSwaggerCommandResponse>(command);
            return Ok(ApiResponse<ParseSwaggerCommandResponse>.Success(response));
        }

        /// <summary>
        /// 4. 获取 MCP 服务器分页列表 (OData)
        /// </summary>
        [HttpGet("servers")]
        [EndpointSummary("服务列表")]
        [Authorize]
        [RequirePermission("Mcp.Servers.Read")]
        public async Task<IActionResult> GetServers([FromServices] IMcpDbContext db)
        {
            var query = db.McpServers.ProjectToType<McpServerDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<McpServerDto>("McpServers");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(McpServerDto), null);
            var odataQuery = new ODataQueryOptions<McpServerDto>(odataContext, Request);

            var filteredQuery = (IQueryable<McpServerDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

            long totalCount = await filteredQuery.LongCountAsync();

            if (odataQuery.Skip != null)
                filteredQuery = filteredQuery.Skip(odataQuery.Skip.Value);
            
            if (odataQuery.Top != null)
                filteredQuery = filteredQuery.Take(odataQuery.Top.Value);

            var items = await filteredQuery.ToArrayAsync();

            int? top = odataQuery.Top?.Value;
            int? skip = odataQuery.Skip?.Value;
            int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;

            var pagedResult = new PagedResult<McpServerDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return Ok(ApiResponse<PagedResult<McpServerDto>>.Success(pagedResult));
        }

        /// <summary>
        /// 5. 获取 MCP 工具分页列表 (OData)
        /// </summary>
        [HttpGet("tools")]
        [EndpointSummary("工具列表")]
        [Authorize]
        [RequirePermission("Mcp.Tools.Read")]
        public async Task<IActionResult> GetTools([FromServices] IMcpDbContext db)
        {
            var query = db.McpTools.ProjectToType<McpToolDto>();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<McpToolDto>("McpTools");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(McpToolDto), null);
            var odataQuery = new ODataQueryOptions<McpToolDto>(odataContext, Request);

            var filteredQuery = (IQueryable<McpToolDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

            long totalCount = await filteredQuery.LongCountAsync();

            if (odataQuery.Skip != null)
                filteredQuery = filteredQuery.Skip(odataQuery.Skip.Value);
            
            if (odataQuery.Top != null)
                filteredQuery = filteredQuery.Take(odataQuery.Top.Value);

            var items = await filteredQuery.ToArrayAsync();

            int? top = odataQuery.Top?.Value;
            int? skip = odataQuery.Skip?.Value;
            int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;

            var pagedResult = new PagedResult<McpToolDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return Ok(ApiResponse<PagedResult<McpToolDto>>.Success(pagedResult));
        }
    }
}
