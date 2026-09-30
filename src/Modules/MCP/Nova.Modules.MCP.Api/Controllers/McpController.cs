using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;
using Nova.Framework.Web.Controllers;
using Nova.Framework.Web.Responses;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.Dtos;
using Nova.Modules.Mcp.Api.Common;
using Nova.Contracts.Security;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ImportOpenApi;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ParseSwagger;
using System.Linq;

namespace Nova.Modules.Mcp.Api.Controllers
{
    /// <summary>
    /// MCP 管理控制器（仅限管理员，JWT + 权限鉴权）。
    /// 提供 Swagger 导入、服务器 / 工具列表等管理类端点。
    ///
    /// 第三方 AI 客户端使用的 MCP 协议网关（SSE / JSON-RPC）已拆分到
    /// <see cref="McpGatewayController"/>，以独立的 MCP Key 鉴权，不再与本控制器混用。
    ///
    /// 分层约定：本控制器属于最外层（Web API），只做「协议转换 + 编排调用」，
    /// 不含任何业务规则与数据访问实现——查询与分页由 Application 层契约
    /// （<see cref="IMcpPagedQueryService{TDto}"/>）委托给 Infrastructure 层实现。
    /// </summary>
    [ApiController]
    [Route("api/mcp")]
    [Authorize]
    [Tags("Mcp")]
    public class McpController : NovaControllerBase
    {
        /// <summary>
        /// 1. 从前端管理员面板导入 Swagger JSON
        /// 负责将现有的 Web API 自动转化为 MCP 代理工具
        /// </summary>
        [HttpPost("import")]
        [EndpointSummary("导入配置")]
        [RequirePermission("Mcp.Servers.Import")]
        public async Task<IActionResult> ImportOpenApi([FromBody] ImportOpenApiCommand command)
        {
            // 拉取数兆 Swagger 文档 + 生成工具链耗时较长，放宽中介请求超时（默认 30s 会提前中止）
            var response = await SendRequestAsync<ImportOpenApiCommand, ImportOpenApiCommandResponse>(command, TimeSpan.FromMinutes(5));
            return Ok(new { success = true, serverId = response.ServerId });
        }

        /// <summary>
        /// 2. 解析 Swagger（地址 / JSON 二选一）
        /// 返回最终用于解析的 Swagger JSON 与全部接口操作，供前端回填与勾选
        /// </summary>
        [HttpPost("parse")]
        [EndpointSummary("解析接口")]
        [RequirePermission("Mcp.Servers.Import")]
        public async Task<IActionResult> ParseSwagger([FromBody] ParseSwaggerCommand command)
        {
            // 远端 Swagger 文档可能数兆、伴随网络延迟，放宽中介请求超时（默认 30s 会提前中止）
            var response = await SendRequestAsync<ParseSwaggerCommand, ParseSwaggerCommandResponse>(command, TimeSpan.FromMinutes(5));
            return Ok(ApiResponse<ParseSwaggerCommandResponse>.Success(response));
        }

        /// <summary>
        /// 3. 获取 MCP 服务器分页列表 (OData)
        /// </summary>
        [HttpGet("servers")]
        [EndpointSummary("服务列表")]
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
        /// 4. 获取 MCP 工具分页列表 (OData)
        /// </summary>
        [HttpGet("tools")]
        [EndpointSummary("工具列表")]
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
