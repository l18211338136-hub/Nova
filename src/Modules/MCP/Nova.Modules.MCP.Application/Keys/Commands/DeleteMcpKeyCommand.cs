using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 删除 MCP 访问密钥（软删除，由框架审计拦截器改写 IsDeleted）。
/// </summary>
[Description("删除 MCP 访问密钥")]
[ApiEndpoint("DELETE", "/api/mcp/keys/{id}", typeof(DeleteMcpKeyResult), "McpKeys", Summary = "删除 MCP 密钥")]
[RequirePermission("Mcp.Keys.Delete")]
public record DeleteMcpKeyCommand
{
    [FromRoute]
    [Description("密钥 ID")]
    public Guid Id { get; init; }
}

public record DeleteMcpKeyResult
{
    public bool Success { get; init; }
}
