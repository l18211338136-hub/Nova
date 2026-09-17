using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Nova.Modules.Mcp.Application.OpenApi.Commands.ParseSwagger;

namespace Nova.Modules.Mcp.Application.OpenApi.Commands.ParseSwagger;

/// <summary>
/// 解析 Swagger/OpenAPI 文档（URL 与 JSON 二选一）。
/// URL 优先级低于 JSON：JSON 非空直接解析；否则通过服务端拉取 URL 内容（规避浏览器 CORS 限制），
/// 再解析出全部接口操作一并回传，供前端回填与勾选。
/// </summary>
public class ParseSwaggerCommandConsumer : IConsumer<ParseSwaggerCommand>
{
    private static readonly string[] OperationMethods =
        ["get", "post", "put", "delete", "patch", "head", "options"];

    private readonly IHttpClientFactory _httpClientFactory;

    public ParseSwaggerCommandConsumer(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task Consume(ConsumeContext<ParseSwaggerCommand> context)
    {
        var request = context.Message;

        // 1. 二选一校验：任填其一即可
        var hasJson = !string.IsNullOrWhiteSpace(request.SwaggerJson);
        var hasUrl = !string.IsNullOrWhiteSpace(request.SwaggerUrl);
        if (!hasJson && !hasUrl)
        {
            throw new InvalidOperationException("SwaggerUrl 与 SwaggerJson 至少需要填写一项。");
        }

        // 2. 解析文档文本：JSON 优先，否则由服务端拉取 SwaggerUrl
        string swaggerJson = hasJson
            ? request.SwaggerJson!.Trim()
            : await FetchSwaggerAsync(request.SwaggerUrl!, request.AuthToken, context.CancellationToken);

        // 3. 解析全部接口操作
        var operations = ParseOperations(swaggerJson);
        if (operations.Count == 0)
        {
            throw new InvalidOperationException("文档中未找到任何接口（paths 为空）。");
        }

        // 4. Request/Response 回写
        if (context.RequestId != null)
        {
            await context.RespondAsync(new ParseSwaggerCommandResponse
            {
                SwaggerJson = swaggerJson,
                Operations = operations
            });
        }
    }

    /// <summary>由服务端拉取远端 Swagger 文档，可选携带 Bearer 令牌。</summary>
    private async Task<string> FetchSwaggerAsync(string swaggerUrl, string? authToken, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(swaggerUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("SwaggerUrl 不是合法的绝对地址。");
        }

        var client = _httpClientFactory.CreateClient("McpDynamicClient");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Trim());
        }

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"拉取 Swagger 文档失败：{(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("远端 Swagger 文档内容为空。");
        }

        return content;
    }

    /// <summary>从 OpenAPI/Swagger JSON 中解析全部接口操作（兼容 OpenAPI 3 与 Swagger 2.0 的 paths 结构）。</summary>
    private static List<ParsedSwaggerOperationDto> ParseOperations(string swaggerJson)
    {
        using var doc = JsonDocument.Parse(swaggerJson);
        if (!doc.RootElement.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var operations = new List<ParsedSwaggerOperationDto>();
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Value.ValueKind != JsonValueKind.Object) continue;

            foreach (var method in OperationMethods)
            {
                if (!path.Value.TryGetProperty(method, out var op) || op.ValueKind != JsonValueKind.Object) continue;

                operations.Add(new ParsedSwaggerOperationDto
                {
                    Method = method.ToUpperInvariant(),
                    Path = path.Name,
                    Summary = GetSummary(op),
                    Group = DeriveGroup(path.Name, GetFirstTag(op))
                });
            }
        }

        return operations;
    }

    private static string GetSummary(JsonElement operation)
    {
        if (operation.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(summary.GetString()))
        {
            return summary.GetString()!;
        }

        if (operation.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
        {
            return description.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? GetFirstTag(JsonElement operation)
    {
        if (!operation.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var tag in tags.EnumerateArray())
        {
            if (tag.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(tag.GetString()))
            {
                return tag.GetString();
            }

            if (tag.ValueKind == JsonValueKind.Object
                && tag.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(name.GetString()))
            {
                return name.GetString();
            }
        }

        return null;
    }

    /// <summary>分组名：优先取 tags[0]，否则按路径前缀（/api/App/user → /api/App）。</summary>
    private static string DeriveGroup(string path, string? tag)
    {
        if (!string.IsNullOrWhiteSpace(tag)) return tag;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return "/";

        if (segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) && segments.Length > 1)
        {
            return $"/{segments[0]}/{segments[1]}";
        }

        return $"/{segments[0]}";
    }
}
