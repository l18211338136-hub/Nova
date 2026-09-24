using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nova.Modules.Mcp.Application.Common.Interfaces;

/// <summary>
/// MCP 访问密钥服务契约（Application 层定义，Infrastructure 层实现）。
/// 供 MCP 网关（SSE / JSON-RPC）做免登录鉴权使用。
/// </summary>
public interface IMcpKeyService
{
    /// <summary>
    /// 从 HTTP 请求中提取 MCP Key。
    /// 支持三种来源：query (?key=)、X-MCP-Key 请求头、Authorization: Bearer。
    /// </summary>
    string? ExtractKeyFromRequest(HttpRequest request);

    /// <summary>
    /// 校验密钥是否有效。
    /// 外部访问没有登录态与租户上下文，实现方需忽略租户/软删除过滤器，
    /// 并按领域规则（<see cref="Domain.Entities.McpKey.IsActive"/>）判定。
    /// </summary>
    Task<bool> ValidateAsync(string key, CancellationToken cancellationToken = default);
}
