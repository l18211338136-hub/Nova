using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;
using Nova.Modules.Mcp.Application.Dtos;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 更新 MCP 访问密钥名称与授权范围。密钥值不可变更。
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

    [Description("是否授权全部服务（为 false 时由 Scopes 指定）")]
    public bool AllServers { get; init; } = true;

    [Description("授权服务与工具明细")]
    public List<McpKeyScopeDto>? Scopes { get; init; }
}

public record UpdateMcpKeyResult
{
    public Guid Id { get; init; }
}
