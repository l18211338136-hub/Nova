using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;
using Nova.Modules.Mcp.Application.Dtos;

namespace Nova.Modules.Mcp.Application.Servers.Commands;

/// <summary>
/// 更新 MCP 服务基本信息。
/// 注意：<c>AuthToken</c> 缺省或留空时沿用原值，不得以 <c>null</c> 覆盖已有令牌。
/// </summary>
[Description("更新 MCP 服务")]
[ApiEndpoint("PUT", "/api/mcp/servers/{id}", typeof(McpServerDto), "Mcp", Summary = "更新 MCP 服务")]
[RequirePermission("Mcp.Servers.Update")]
public record UpdateMcpServerCommand
{
    [FromRoute]
    [Description("MCP 服务 ID")]
    public Guid Id { get; init; }

    [Description("服务名称")]
    public string Name { get; init; } = default!;

    [Description("服务基础地址")]
    public string BaseUrl { get; init; } = default!;

    [Description("Swagger 文档地址")]
    public string? SwaggerUrl { get; init; }

    [Description("认证令牌（留空则保持原令牌不变）")]
    public string? AuthToken { get; init; }
}
