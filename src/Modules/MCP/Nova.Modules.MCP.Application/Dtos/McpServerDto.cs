using System;
using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Dtos;

[Description("MCP 大模型服务")]
[RequirePermission("Mcp.Servers.Read")]
public class McpServerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string BaseUrl { get; set; } = default!;
    public string? SwaggerUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
