using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;
using Nova.Modules.Mcp.Application.Dtos;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 创建 MCP 访问密钥。密钥值由服务端一次性生成，客户端提供名称与授权范围。
/// </summary>
[Description("创建 MCP 访问密钥")]
[ApiEndpoint("POST", "/api/mcp/keys", typeof(CreateMcpKeyResult), "McpKeys", Summary = "创建 MCP 密钥")]
[RequirePermission("Mcp.Keys.Create")]
public record CreateMcpKeyCommand
{
    [Description("密钥名称")]
    public string Name { get; init; } = default!;

    [Description("是否授权全部服务（为 false 时由 Scopes 指定）")]
    public bool AllServers { get; init; } = true;

    [Description("授权服务与工具明细")]
    public List<McpKeyScopeDto>? Scopes { get; init; }
}

public record CreateMcpKeyResult
{
    public Guid Id { get; init; }

    /// <summary>一次性返回的明文密钥，前端应提示用户立即复制保存。</summary>
    public string KeyValue { get; init; } = default!;
}
