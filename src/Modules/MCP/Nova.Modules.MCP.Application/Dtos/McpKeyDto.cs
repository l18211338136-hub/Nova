using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Dtos;

/// <summary>
/// MCP 密钥的单服务授权范围。
/// <c>ToolIds == null</c> 表示授权该服务下的全部工具。
/// </summary>
public class McpKeyScopeDto
{
    public Guid ServerId { get; set; }

    public List<Guid>? ToolIds { get; set; }
}

/// <summary>
/// MCP 访问密钥 DTO（明文 KeyValue，前端负责遮罩与复制）。
/// </summary>
[Description("MCP 访问密钥")]
[RequirePermission("Mcp.Keys.Read")]
public class McpKeyDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>密钥明文（永久有效，由服务端生成）。</summary>
    public string KeyValue { get; set; } = default!;

    /// <summary>是否授权访问全部服务；为 false 时由 <see cref="Scopes"/> 约束。</summary>
    public bool AllServers { get; set; } = true;

    public List<McpKeyScopeDto> Scopes { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; }
}
