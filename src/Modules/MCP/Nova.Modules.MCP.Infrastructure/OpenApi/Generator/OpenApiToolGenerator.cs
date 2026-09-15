using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Domain.ValueObjects;

namespace Nova.Modules.Mcp.Infrastructure.OpenApi.Generator
{
    public class OpenApiToolGenerator : IOpenApiToolGenerator
    {
        public List<McpToolDefinition> GenerateToolsFromSwagger(string swaggerJson, string baseUrl)
        {
            var reader = new OpenApiStringReader();
            var document = reader.Read(swaggerJson, out var diagnostic);
            var tools = new List<McpToolDefinition>();

            if (document.Paths == null) return tools;

            foreach (var pathItem in document.Paths)
            {
                foreach (var operation in pathItem.Value.Operations)
                {
                    var op = operation.Value;
                    if (op.Deprecated) continue;

                    var method = operation.Key.ToString().ToUpper();
                    
                    string toolName = op.OperationId?.ToLower() ?? string.Empty;
                    if (string.IsNullOrEmpty(toolName))
                    {
                        toolName = $"{method}_{pathItem.Key}".Replace("/", "_").Replace("{", "").Replace("}", "").ToLower();
                    }

                    string description = string.IsNullOrWhiteSpace(op.Summary) ? 
                        (op.Description ?? $"Execute {method} to {pathItem.Key}") : op.Summary;

                    var mcpProperties = new Dictionary<string, object>();
                    var mcpRequired = new List<string>();
                    var profile = new ExecutionProfile
                    {
                        TargetUrl = baseUrl.TrimEnd('/') + pathItem.Key,
                        TargetMethod = method
                    };

                    if (op.Parameters != null)
                    {
                        foreach (var param in op.Parameters)
                        {
                            string paramName = param.Name;
                            string type = MapSchemaType(param.Schema?.Type);
                            
                            mcpProperties[paramName] = new { type = type, description = param.Description };
                            if (param.Required) mcpRequired.Add(paramName);

                            profile.ParameterMap[paramName] = new ParameterMapping
                            {
                                TargetKey = paramName,
                                In = param.In.ToString()?.ToLower() ?? "query",
                                Type = type
                            };
                        }
                    }

                    if (op.RequestBody?.Content != null && op.RequestBody.Content.TryGetValue("application/json", out var mediaType))
                    {
                        var bodySchema = mediaType.Schema;
                        if (bodySchema?.Properties != null)
                        {
                            foreach (var prop in bodySchema.Properties)
                            {
                                string propName = prop.Key;
                                string type = MapSchemaType(prop.Value.Type);
                                
                                mcpProperties[propName] = new { type = type, description = prop.Value.Description };
                                
                                profile.ParameterMap[propName] = new ParameterMapping
                                {
                                    TargetKey = propName,
                                    In = "body",
                                    Type = type
                                };
                            }

                            if (bodySchema.Required != null)
                            {
                                mcpRequired.AddRange(bodySchema.Required);
                            }
                        }
                    }

                    tools.Add(new McpToolDefinition
                    {
                        Name = toolName,
                        Description = description,
                        Profile = profile,
                        InputSchema = new
                        {
                            type = "object",
                            properties = mcpProperties,
                            required = mcpRequired.Distinct().ToList()
                        }
                    });
                }
            }

            return tools;
        }

        private string MapSchemaType(string? openApiType)
        {
            if (string.IsNullOrEmpty(openApiType)) return "string";
            
            return openApiType.ToLower() switch
            {
                "integer" => "integer",
                "number" => "number",
                "boolean" => "boolean",
                "array" => "array",
                "object" => "object",
                _ => "string"
            };
        }
    }
}
