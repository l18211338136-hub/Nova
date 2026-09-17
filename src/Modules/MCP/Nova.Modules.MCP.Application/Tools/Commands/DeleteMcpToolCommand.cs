using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Tools.Commands;

/// <summary>
/// 删除单个 MCP 工具。
/// </summary>
[Description("删除 MCP 工具")]
[ApiEndpoint("DELETE", "/api/mcp/tools/{id}", typeof(DeleteMcpToolResult), "Mcp", Summary = "删除 MCP 工具")]
[RequirePermission("Mcp.Tools.Delete")]
public record DeleteMcpToolCommand
{
    [FromRoute]
    [Description("MCP 工具 ID")]
    public Guid Id { get; init; }
}

public record DeleteMcpToolResult
{
    public bool Success { get; init; }
}
