using System;
using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Dtos;

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

    public DateTimeOffset CreatedAt { get; set; }
}
