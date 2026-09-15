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

        public HttpToolExecutor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> ExecuteAsync(ExecutionProfile profile, JsonNode llmArguments, CancellationToken cancellationToken = default)
        {
            string requestUrl = profile.TargetUrl;
            var queryParams = new List<string>();
            var bodyParams = new Dictionary<string, object>();
            
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

            if (!string.IsNullOrEmpty(profile.AuthToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", profile.AuthToken);
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
