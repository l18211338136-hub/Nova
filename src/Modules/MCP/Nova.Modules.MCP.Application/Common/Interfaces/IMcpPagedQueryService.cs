using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nova.Contracts.Responses;

namespace Nova.Modules.Mcp.Application.Common.Interfaces;

/// <summary>
/// MCP 模块的分页查询服务契约（Application 层定义，Infrastructure 层实现）。
/// 把「数据源 + 分页执行」从最外层（Controller / 端点）下沉到基础设施层，
/// 最外层只负责 HTTP 协议相关的事情（OData 参数解析、响应包装）。
/// </summary>
public interface IMcpPagedQueryService<TDto> where TDto : class
{
    /// <summary>
    /// 返回可组合的 DTO 查询源（已投影，未执行）。
    /// </summary>
    IQueryable<TDto> Query();

    /// <summary>
    /// 对已过滤/排序的查询执行分页，返回统一分页结果。
    /// </summary>
    Task<PagedResult<TDto>> ToPagedResultAsync(
        IQueryable<TDto> query,
        int? skip,
        int? top,
        CancellationToken cancellationToken = default);
}
