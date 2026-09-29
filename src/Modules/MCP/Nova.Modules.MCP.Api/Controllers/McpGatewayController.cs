using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nova.Modules.Mcp.Application.Common.Interfaces;

namespace Nova.Modules.Mcp.Api.Controllers
{
    /// <summary>
    /// MCP 协议网关（第三方 AI 客户端专用，仅以 MCP Key 鉴权，不走 JWT / 权限体系）。
    ///
    /// 这里只暴露 MCP 传输层所需的两个端点：
    ///   - GET  /api/mcp/sse       建立 SSE 长连接
    ///   - POST /api/mcp/messages  接收 JSON-RPC 消息
    /// tools/list、tools/call 等 JSON-RPC 方法均在该通道内完成，无需额外端点。
    ///
    /// 与管理端点（<see cref="McpController"/>，JWT + 权限）物理隔离：第三方客户端
    /// 拿一个 MCP Key 即可访问本控制器，绝不会触碰任何管理接口，也避免管理接口
    /// 混入免登录的密钥鉴权逻辑，消除「复用接口造成的混乱」。
    ///
    /// 分层约定：最外层（Web API）只做「协议转换 + 编排」，
    /// 密钥提取/校验交由 <see cref="IMcpKeyService"/>（Application 契约 + Infrastructure 实现）。
    /// </summary>
    [ApiController]
    [Route("api/mcp")]
    [Tags("McpGateway")]
    public class McpGatewayController : ControllerBase
    {
        private readonly IMcpServerEngine _mcpEngine;
        private readonly IMcpKeyService _mcpKeyService;

        public McpGatewayController(IMcpServerEngine mcpEngine, IMcpKeyService mcpKeyService)
        {
            _mcpEngine = mcpEngine;
            _mcpKeyService = mcpKeyService;
        }

        /// <summary>
        /// 1. 建立 SSE (Server-Sent Events) 长连接
        /// 第三方大模型 (如 Claude Desktop) 首先会调用此接口，并以 MCP Key 鉴权。
        /// </summary>
        [HttpGet("sse")]
        [EndpointSummary("建立连接")]
        public async Task GetSseConnection()
        {
            if (!await EnsureKeyAuthenticatedAsync())
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
            string? postEndpoint = Url.Action(nameof(PostMessage), "McpGateway", new { sessionId }, Request.Scheme);

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

            if (!await EnsureKeyAuthenticatedAsync())
            {
                return StatusCode(StatusCodes.Status401Unauthorized, "Unauthorized: 缺少或无效的 MCP Key。");
            }

            using var reader = new StreamReader(Request.Body);
            string jsonRpcMessage = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonRpcMessage))
            {
                return BadRequest("Empty message.");
            }

            // 提取 MCP Key 明文并透传给引擎，供「创建者关联」判定（公开工具 ∪ 该 Key 创建者自己的工具）。
            var mcpKey = _mcpKeyService.ExtractKeyFromRequest(Request);

            // 将收到的消息扔给底层的 MCP 引擎，引擎处理完后自动通过 SSE 回推
            bool success = await _mcpEngine.HandleMessageAsync(sessionId, mcpKey, jsonRpcMessage);
            if (!success)
            {
                return NotFound("Session not found or expired.");
            }

            return Accepted();
        }

        /// <summary>
        /// 以 MCP Key 完成免登录鉴权：从请求中提取密钥并校验有效性。
        /// 校验失败返回 false，由调用方决定如何响应。
        /// </summary>
        private async Task<bool> EnsureKeyAuthenticatedAsync()
        {
            var key = _mcpKeyService.ExtractKeyFromRequest(Request);
            return !string.IsNullOrWhiteSpace(key)
                && await _mcpKeyService.ValidateAsync(key, HttpContext.RequestAborted);
        }
    }
}
