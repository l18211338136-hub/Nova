using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Domain.Entities;

namespace Nova.Modules.Mcp.Infrastructure.Services;

/// <summary>
/// MCP 访问密钥服务的基础设施实现（Application 层只定义契约，具体实现在本层）。
/// </summary>
public class McpKeyService : IMcpKeyService
{
    private const string QueryKeyName = "key";
    private const string HeaderName = "X-MCP-Key";
    private const string BearerPrefix = "Bearer ";

    private readonly IMcpDbContext _dbContext;

    public McpKeyService(IMcpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string? ExtractKeyFromRequest(HttpRequest request)
    {
        if (request is null) return null;

        if (request.Query.TryGetValue(QueryKeyName, out var fromQuery) &&
            !string.IsNullOrWhiteSpace(fromQuery))
        {
            return fromQuery.ToString().Trim();
        }

        if (request.Headers.TryGetValue(HeaderName, out var fromHeader) &&
            !string.IsNullOrWhiteSpace(fromHeader))
        {
            return fromHeader.ToString().Trim();
        }

        var authorization = request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) &&
            authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[BearerPrefix.Length..].Trim();
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        // 外部访问（SSE / JSON-RPC）没有登录态与租户上下文：
        // 忽略全局查询过滤器（租户隔离 + 软删除），仅按密钥本身匹配。
        var matched = await _dbContext.McpKeys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyValue == key, cancellationToken);

        if (matched is null) return false;

        // 过期与软删除判定交由领域实体自身的业务规则处理
        return matched.IsActive(DateTimeOffset.UtcNow);
    }
}
