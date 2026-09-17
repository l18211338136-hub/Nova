using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Nova.Modules.Mcp.Domain.ValueObjects;

namespace Nova.Modules.Mcp.Infrastructure.Providers.Http
{
    public class HttpToolExecutor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public HttpToolExecutor(IHttpClientFactory httpClientFactory, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
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

                    if (paramValue == null || !profile.ParameterMap.TryGetValue(mcpKey, out var mapping))
                    {
                        continue;
                    }

                    string rawValue = paramValue.ToString();

                    switch (mapping.In.ToLower())
                    {
                        case "path":
                            requestUrl = requestUrl.Replace($"{{{mapping.TargetKey}}}", rawValue);
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

            if (queryParams.Any())
            {
                requestUrl += "?" + string.Join("&", queryParams);
            }

            var request = new HttpRequestMessage(new HttpMethod(profile.TargetMethod), requestUrl);

            if (bodyParams.Any())
            {
                string jsonBody = JsonSerializer.Serialize(bodyParams);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            // ===== 身份透传方案（网关层拦截） =====
            // 优先提取双 Token 架构下的专门透传业务 Token
            var userToken = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-Authorization"].ToString();
            
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

            var client = _httpClientFactory.CreateClient("McpDynamicClient");
            var response = await client.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return $"API Error ({(int)response.StatusCode}): {errorContent}";
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        private object GetRealValue(JsonNode node)
        {
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
