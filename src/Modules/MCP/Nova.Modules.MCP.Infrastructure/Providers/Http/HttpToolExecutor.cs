using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nova.Modules.Mcp.Domain.ValueObjects;

namespace Nova.Modules.Mcp.Infrastructure.Providers.Http
{
    public class HttpToolExecutor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HttpToolExecutor> _logger;

        public HttpToolExecutor(
            IHttpClientFactory httpClientFactory,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
            ILogger<HttpToolExecutor> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<string> ExecuteAsync(ExecutionProfile profile, JsonNode llmArguments, CancellationToken cancellationToken = default)
        {
            string requestUrl = profile.TargetUrl;
            var queryParams = new List<string>();
            var bodyParams = new Dictionary<string, object>();
            var headerParams = new Dictionary<string, string>();

            if (llmArguments is JsonObject argsObject)
            {
                foreach (var arg in argsObject)
                {
                    string mcpKey = arg.Key;
                    var paramValue = arg.Value;

                    if (paramValue == null)
                    {
                        continue;
                    }

                    if (!profile.ParameterMap.TryGetValue(mcpKey, out var mapping))
                    {
                        mapping = profile.ParameterMap
                            .FirstOrDefault(kv => string.Equals(kv.Key, mcpKey, StringComparison.OrdinalIgnoreCase))
                            .Value;
                        if (mapping == null)
                        {
                            continue;
                        }
                    }

                    string rawValue = paramValue.ToString();

                    // 只要 URL 模板带有该参数的占位符，优先强制替换 URL 占位符（忽略大小写，兼容历史仅标记为 body 的映射）
                    requestUrl = ReplacePathPlaceholder(requestUrl, mapping.TargetKey, mcpKey, rawValue);

                    // 支持通过 "|" 组合的多位置映射（例如 "path|body"）
                    var locations = (mapping.In ?? string.Empty)
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var loc in locations)
                    {
                        switch (loc.ToLowerInvariant())
                        {
                            case "path":
                                requestUrl = ReplacePathPlaceholder(requestUrl, mapping.TargetKey, mcpKey, rawValue);
                                break;

                            case "query":
                                queryParams.Add($"{mapping.TargetKey}={Uri.EscapeDataString(rawValue)}");
                                break;

                            case "body":
                                bodyParams[mapping.TargetKey] = GetRealValue(paramValue);
                                break;

                            case "header":
                                headerParams[mapping.TargetKey] = rawValue;
                                break;
                        }
                    }
                }
            }

            if (queryParams.Any())
            {
                requestUrl += "?" + string.Join("&", queryParams);
            }

            var request = new HttpRequestMessage(new HttpMethod(profile.TargetMethod), requestUrl);

            string? jsonBody = null;
            if (bodyParams.Any())
            {
                jsonBody = JsonSerializer.Serialize(bodyParams);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            // ===== 身份透传方案（网关层拦截） =====
            // 优先提取双 Token 架构下的专门透传业务 Token
            var userToken = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-Authorization"].ToString();

            if (!string.IsNullOrWhiteSpace(userToken) && !userToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                userToken = $"Bearer {userToken}";
            }

            if (!string.IsNullOrEmpty(userToken))
            {
                // 如果有专门透传的用户 Token，强制覆盖发给业务 API
                request.Headers.Remove("Authorization");
                request.Headers.TryAddWithoutValidation("Authorization", userToken);
            }
            else
            {
                // 否则，兜底使用当前请求头自带的 Authorization（如单系统测试客户端）
                var userAuthHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(userAuthHeader))
                {
                    request.Headers.Remove("Authorization");
                    request.Headers.TryAddWithoutValidation("Authorization", userAuthHeader);
                }
            }
            // ==========================================

            foreach (var header in headerParams)
            {
                // 如果网关层已经透传了真正的 Authorization Token，则丢弃任何由参数映射过来的 Authorization，防止重复/覆盖
                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase) &&
                    request.Headers.Contains("Authorization"))
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var httpContext = _httpContextAccessor.HttpContext;
            string clientIp = GetClientIp(httpContext);
            string inboundUrl = httpContext != null
                ? $"{httpContext.Request.Method} {httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}"
                : "(none)";
            string headersSummary = FormatHeadersForLog(request.Headers);

            _logger.LogInformation(
                "MCP HTTP Request =>\n  ClientIp: {ClientIp}\n  InboundUrl: {InboundUrl}\n  TargetUrl: {Method} {Url}\n  Headers: {Headers}\n  Body: {Body}",
                clientIp,
                inboundUrl,
                profile.TargetMethod,
                requestUrl,
                headersSummary,
                jsonBody ?? "(empty)");

            var sw = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient("McpDynamicClient");
            var response = await client.SendAsync(request, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MCP HTTP Response <= {StatusCode} ({ElapsedMs}ms)\n  ClientIp: {ClientIp}\n  TargetUrl: {Method} {Url}\n  ErrorBody: {ResponseBody}",
                    (int)response.StatusCode,
                    sw.ElapsedMilliseconds,
                    clientIp,
                    profile.TargetMethod,
                    requestUrl,
                    responseContent);
                return $"API Error ({(int)response.StatusCode}): {responseContent}";
            }

            _logger.LogInformation(
                "MCP HTTP Response <= {StatusCode} ({ElapsedMs}ms)\n  ClientIp: {ClientIp}\n  TargetUrl: {Method} {Url}\n  ResponseBody: {ResponseBody}",
                (int)response.StatusCode,
                sw.ElapsedMilliseconds,
                clientIp,
                profile.TargetMethod,
                requestUrl,
                responseContent);

            return responseContent;
        }

        private static string GetClientIp(Microsoft.AspNetCore.Http.HttpContext? context)
        {
            if (context == null) return "(unknown)";

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstIp))
                {
                    return firstIp;
                }
            }

            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp.Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "(unknown)";
        }

        private static string FormatHeadersForLog(HttpRequestHeaders headers)
        {
            var items = new List<string>();
            foreach (var h in headers)
            {
                string val = string.Join(",", h.Value);
                if (string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase) && val.Length > 24)
                {
                    val = $"{val[..24]}...(len={val.Length})";
                }
                items.Add($"{h.Key}={val}");
            }
            return items.Count > 0 ? string.Join("; ", items) : "(none)";
        }

        private static string ReplacePathPlaceholder(string requestUrl, string targetKey, string mcpKey, string rawValue)
        {
            string encodedValue = Uri.EscapeDataString(rawValue);

            if (!string.IsNullOrEmpty(targetKey) &&
                requestUrl.Contains($"{{{targetKey}}}", StringComparison.OrdinalIgnoreCase))
            {
                return Regex.Replace(
                    requestUrl,
                    $"\\{{{Regex.Escape(targetKey)}\\}}",
                    _ => encodedValue,
                    RegexOptions.IgnoreCase);
            }

            if (!string.IsNullOrEmpty(mcpKey) &&
                requestUrl.Contains($"{{{mcpKey}}}", StringComparison.OrdinalIgnoreCase))
            {
                return Regex.Replace(
                    requestUrl,
                    $"\\{{{Regex.Escape(mcpKey)}\\}}",
                    _ => encodedValue,
                    RegexOptions.IgnoreCase);
            }

            return requestUrl;
        }

        private object GetRealValue(JsonNode? node)
        {
            if (node == null) return string.Empty;

            if (node is JsonValue value)
            {
                if (value.TryGetValue<int>(out int i)) return i;
                if (value.TryGetValue<double>(out double d)) return d;
                if (value.TryGetValue<bool>(out bool b)) return b;
                return value.ToString();
            }
            if (node is JsonArray array)
            {
                return array.Select(GetRealValue).ToList();
            }
            if (node is JsonObject obj)
            {
                var dict = new Dictionary<string, object>();
                foreach (var kv in obj)
                {
                    if (kv.Value != null) dict[kv.Key] = GetRealValue(kv.Value);
                }
                return dict;
            }
            return node.ToString();
        }
    }
}
