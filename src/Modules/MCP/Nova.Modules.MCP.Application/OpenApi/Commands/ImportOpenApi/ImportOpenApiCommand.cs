using System;
using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.OpenApi.Commands.ImportOpenApi;

[Description("MCP 大模型服务")]
[RequirePermission("Mcp.Servers.Import")]
public class ImportOpenApiCommand
{
    public string ServerName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public string? SwaggerUrl { get; set; }
    public string? AuthToken { get; set; }
}

public class ImportOpenApiCommandResponse
{
    public Guid ServerId { get; set; }
}
