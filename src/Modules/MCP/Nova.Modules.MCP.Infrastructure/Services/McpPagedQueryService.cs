using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Responses;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.Dtos;

namespace Nova.Modules.Mcp.Infrastructure.Services;

/// <summary>
/// 分页查询服务的抽象基类：封装「执行分页」这一基础设施细节（EF Core 查询执行）。
/// 具体数据源由子类提供，Application 层只看到 <see cref="IMcpPagedQueryService{TDto}"/> 契约。
/// </summary>
public abstract class McpPagedQueryService<TDto> : IMcpPagedQueryService<TDto> where TDto : class
{
    protected readonly IMcpDbContext DbContext;

    protected McpPagedQueryService(IMcpDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <inheritdoc />
    public abstract IQueryable<TDto> Query();

    /// <inheritdoc />
    public async Task<PagedResult<TDto>> ToPagedResultAsync(
        IQueryable<TDto> query,
        int? skip,
        int? top,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.LongCountAsync(cancellationToken);

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (top.HasValue)
            query = query.Take(top.Value);

        var items = await query.ToArrayAsync(cancellationToken);

        int? page = (skip.HasValue && top.HasValue && top.Value > 0)
            ? (skip.Value / top.Value) + 1
            : 1;

        return new PagedResult<TDto>
        {
            Total = totalCount,
            Items = items,
            Page = page,
            PageSize = top > 0 ? top : null
        };
    }
}

/// <summary>MCP 密钥的分页查询实现。</summary>
public class McpKeyQueryService : McpPagedQueryService<McpKeyDto>
{
    public McpKeyQueryService(IMcpDbContext dbContext) : base(dbContext) { }

    public override IQueryable<McpKeyDto> Query() =>
        DbContext.McpKeys.Select(k => new McpKeyDto
        {
            Id = k.Id,
            Name = k.Name,
            KeyValue = k.KeyValue,
            CreatedAt = k.CreatedAt
        });
}

/// <summary>MCP 服务的分页查询实现。</summary>
public class McpServerQueryService : McpPagedQueryService<McpServerDto>
{
    public McpServerQueryService(IMcpDbContext dbContext) : base(dbContext) { }

    public override IQueryable<McpServerDto> Query() =>
        DbContext.McpServers.Select(s => new McpServerDto
        {
            Id = s.Id,
            Name = s.Name,
            BaseUrl = s.BaseUrl,
            SwaggerUrl = s.SwaggerUrl,
            CreatedAt = s.CreatedAt
        });
}

/// <summary>MCP 工具的分页查询实现。</summary>
public class McpToolQueryService : McpPagedQueryService<McpToolDto>
{
    public McpToolQueryService(IMcpDbContext dbContext) : base(dbContext) { }

    public override IQueryable<McpToolDto> Query() =>
        DbContext.McpTools.Select(t => new McpToolDto
        {
            Id = t.Id,
            ServerId = t.ServerId,
            Name = t.Name,
            Description = t.Description,
            HttpMethod = t.HttpMethod,
            RoutePath = t.RoutePath,
            IsEnabled = t.IsEnabled,
            IsPublic = t.IsPublic,
            CreatedAt = t.CreatedAt
        });
}
