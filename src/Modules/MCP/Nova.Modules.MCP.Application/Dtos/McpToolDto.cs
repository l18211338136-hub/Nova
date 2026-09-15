using System;
using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Dtos;

[Description("MCP 大模型服务")]
[RequirePermission("Mcp.Tools.Read")]
public class McpToolDto
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }
    public string RoutePath { get; set; } = default!;
    public string HttpMethod { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
