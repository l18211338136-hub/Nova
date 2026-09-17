using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Servers.Commands;

/// <summary>
/// 删除 MCP 服务（聚合根）。
/// 领域规则：McpTool 从属于 McpServer 聚合，删除服务时其下所有工具必须一并删除。
/// </summary>
[Description("删除 MCP 服务")]
[ApiEndpoint("DELETE", "/api/mcp/servers/{id}", typeof(DeleteMcpServerResult), "Mcp", Summary = "删除 MCP 服务")]
[RequirePermission("Mcp.Servers.Delete")]
public record DeleteMcpServerCommand
{
    [FromRoute]
    [Description("MCP 服务 ID")]
    public Guid Id { get; init; }
}

public record DeleteMcpServerResult
{
    public bool Success { get; init; }

    /// <summary>随服务一并删除的工具数量。</summary>
    public int DeletedToolCount { get; init; }
}
