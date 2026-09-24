using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Modules.Mcp.Api.Common;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.Dtos;

namespace Nova.Modules.Mcp.Api.Controllers
{
    /// <summary>
    /// MCP 访问密钥控制器：负责密钥的 OData 分页查询。
    /// 增删改仍走声明式 [ApiEndpoint] CQRS 命令（见 Application/Keys/Commands）。
    ///
    /// 分层约定：控制器属于最外层（Web API），只做「HTTP 协议 + 编排」——
    /// OData 参数翻译交给 <see cref="McpODataBinder"/>，
    /// 数据源与分页执行由 <see cref="IMcpPagedQueryService{TDto}"/>（Application 契约）
    /// 委托给 Infrastructure 层实现，控制器内不出现任何 EF / 业务规则代码。
    /// </summary>
    [ApiController]
    [Route("api/mcp/keys")]
    [Authorize]
    [Tags("McpKeys")]
    public class McpKeyController : McpControllerBase
    {
        /// <summary>
        /// 获取 MCP 密钥分页列表 (OData)
        /// </summary>
        [HttpGet]
        [EndpointName("GetMcpKeys")]
        [EndpointSummary("MCP 密钥列表")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<McpKeyDto>>), StatusCodes.Status200OK, "application/json")]
        [RequirePermission("Mcp.Keys.Read")]
        public async Task<IActionResult> GetKeys(
            [FromServices] IMcpPagedQueryService<McpKeyDto> queryService)
        {
            // 1) 协议翻译（OData）：最外层职责
            var bound = McpODataBinder.Bind(queryService.Query(), Request);

            // 2) 分页执行：委托给 Infrastructure 层实现
            var paged = await queryService.ToPagedResultAsync(
                bound.Query, bound.Skip, bound.Top, HttpContext.RequestAborted);

            return Ok(ApiResponse<PagedResult<McpKeyDto>>.Success(McpODataBinder.ToWebResult(paged)));
        }
    }
}
