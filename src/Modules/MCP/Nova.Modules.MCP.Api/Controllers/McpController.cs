using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;
using Nova.Framework.Web.Controllers;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ImportOpenApi;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ParseSwagger;
using Nova.Modules.Mcp.Application.Dtos;
using Nova.Modules.Mcp.Api.Common;
using Nova.Framework.Web.Responses;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Api.Controllers
{
    /// <summary>
    /// MCP 协议标准网关控制器
    /// 处理第三方 AI 客户端的 SSE 连接与 JSON-RPC 消息分发
    ///
    /// 分层约定：本控制器属于最外层（Web API），只做「协议转换 + 编排调用」，
    /// 不含任何业务规则与数据访问实现——密钥提取/校验、查询与分页
    /// 均由 Application 层定义的契约（IMcpKeyService / IMcpPagedQueryService）委托给 Infrastructure 层实现。
    /// </summary>
    [ApiController]
    [Route("api/mcp")]
    // [Authorize] // 免登录：第三方客户端以 MCP Key 鉴权（见 IMcpKeyService）
    public class McpController : NovaControllerBase
    {
        private readonly IMcpServerEngine _mcpEngine;
        private readonly IMcpKeyService _mcpKeyService;

        public McpController(IMcpServerEngine mcpEngine, IMcpKeyService mcpKeyService)
        {
            _mcpEngine = mcpEngine;
            _mcpKeyService = mcpKeyService;
        }

        /// <summary>
        /// 1. 建立 SSE (Server-Sent Events) 长连接
        /// 第三方大模型 (如 Claude Desktop) 首先会调用此接口
        /// </summary>
        [HttpGet("sse")]
        [EndpointSummary("建立连接")]
        public async Task GetSseConnection()
        {
            // 免登录访问：以 MCP Key 代替 JWT 鉴权（具体实现在 Infrastructure 层）
            var key = _mcpKeyService.ExtractKeyFromRequest(Request);
            if (string.IsNullOrWhiteSpace(key) || !await _mcpKeyService.ValidateAsync(key, HttpContext.RequestAborted))
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsync("Unauthorized: 缺少或无效的 MCP Key。");
                return;
            }

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

            // 免登录访问：以 MCP Key 代替 JWT 鉴权（具体实现在 Infrastructure 层）
            var key = _mcpKeyService.ExtractKeyFromRequest(Request);
            if (string.IsNullOrWhiteSpace(key) || !await _mcpKeyService.ValidateAsync(key, HttpContext.RequestAborted))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, "Unauthorized: 缺少或无效的 MCP Key。");
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
        [ProducesResponseType(typeof(ApiResponse<PagedResult<McpServerDto>>), StatusCodes.Status200OK, "application/json")]
        public async Task<IActionResult> GetServers(
            [FromServices] IMcpPagedQueryService<McpServerDto> queryService)
        {
            // 1) 协议翻译（OData）：最外层职责
            var bound = McpODataBinder.Bind(queryService.Query(), Request);

            // 2) 分页执行：委托给 Infrastructure 层实现
            var paged = await queryService.ToPagedResultAsync(
                bound.Query, bound.Skip, bound.Top, HttpContext.RequestAborted);

            return Ok(ApiResponse<PagedResult<McpServerDto>>.Success(McpODataBinder.ToWebResult(paged)));
        }

        /// <summary>
        /// 5. 获取 MCP 工具分页列表 (OData)
        /// </summary>
        [HttpGet("tools")]
        [EndpointSummary("工具列表")]
        [Authorize]
        [RequirePermission("Mcp.Tools.Read")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<McpToolDto>>), StatusCodes.Status200OK, "application/json")]
        public async Task<IActionResult> GetTools(
            [FromServices] IMcpPagedQueryService<McpToolDto> queryService)
        {
            // 1) 协议翻译（OData）：最外层职责
            var bound = McpODataBinder.Bind(queryService.Query(), Request);

            // 2) 分页执行：委托给 Infrastructure 层实现
            var paged = await queryService.ToPagedResultAsync(
                bound.Query, bound.Skip, bound.Top, HttpContext.RequestAborted);

            return Ok(ApiResponse<PagedResult<McpToolDto>>.Success(McpODataBinder.ToWebResult(paged)));
        }
    }
}
