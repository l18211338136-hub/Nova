using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Mcp.Application.OpenApi.Commands.ParseSwagger;

[Description("MCP 解析 Swagger")]
[RequirePermission("Mcp.Servers.Import")]
public class ParseSwaggerCommand
{
    /// <summary>Swagger 文档地址（与 SwaggerJson 二选一，Json 为空时由此地址拉取）。</summary>
    public string? SwaggerUrl { get; set; }

    /// <summary>Swagger/OpenAPI JSON 文本（与 SwaggerUrl 二选一，非空时优先使用）。</summary>
    public string? SwaggerJson { get; set; }

    /// <summary>可选：拉取 SwaggerUrl 时携带的 Bearer 令牌。</summary>
    public string? AuthToken { get; set; }
}

public class ParseSwaggerCommandResponse
{
    /// <summary>最终用于解析的 Swagger JSON 文本（URL 拉取后回传，供前端回填展示）。</summary>
    public string SwaggerJson { get; set; } = string.Empty;

    /// <summary>解析出的全部接口操作。</summary>
    public List<ParsedSwaggerOperationDto> Operations { get; set; } = new();
}

public class ParsedSwaggerOperationDto
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    /// <summary>分组名：优先取 OpenAPI tags[0]，否则取路径前缀（如 "/api/App"）。</summary>
    public string Group { get; set; } = string.Empty;
}
