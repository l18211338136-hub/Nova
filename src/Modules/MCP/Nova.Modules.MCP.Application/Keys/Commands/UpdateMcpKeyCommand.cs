using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 更新 MCP 访问密钥。业务约定仅允许修改名称，密钥值不可变更。
/// </summary>
[Description("更新 MCP 访问密钥")]
[ApiEndpoint("PUT", "/api/mcp/keys/{id}", typeof(UpdateMcpKeyResult), "McpKeys", Summary = "更新 MCP 密钥")]
[RequirePermission("Mcp.Keys.Update")]
public record UpdateMcpKeyCommand
{
    [FromRoute]
    [Description("密钥 ID")]
    public Guid Id { get; init; }

    [Description("密钥名称")]
    public string Name { get; init; } = default!;
}

public record UpdateMcpKeyResult
{
    public Guid Id { get; init; }
}
