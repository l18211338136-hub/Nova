using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            // 兼容降级：Microsoft.OpenApi.Readers 低版本不支持 3.1.x，将其替换为 3.0.1 以绕过版本检查
            swaggerJson = Regex.Replace(swaggerJson, @"""openapi""\s*:\s*""3\.1\.[0-9]+""", "\"openapi\": \"3.0.1\"");

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
                    
                    string rawName = string.IsNullOrEmpty(op.OperationId) 
                        ? $"{method}_{pathItem.Key}" 
                        : op.OperationId;
                    
                    string toolName = SanitizeToolName(rawName);

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
                            mcpProperties[paramName] = GetMcpSchema(param.Schema, document);
                            
                            if (param.Required) mcpRequired.Add(paramName);

                            profile.ParameterMap[paramName] = new ParameterMapping
                            {
                                TargetKey = paramName,
                                In = param.In.ToString()?.ToLower() ?? "query",
                                Type = MapSchemaType(param.Schema?.Type)
                            };
                        }
                    }

                    if (op.RequestBody?.Content != null && op.RequestBody.Content.TryGetValue("application/json", out var mediaType))
                    {
                        var bodySchema = ResolveSchema(mediaType.Schema, document);
                        if (bodySchema?.Properties != null)
                        {
                            foreach (var prop in bodySchema.Properties)
                            {
                                string propName = prop.Key;
                                mcpProperties[propName] = GetMcpSchema(prop.Value, document);
                                
                                profile.ParameterMap[propName] = new ParameterMapping
                                {
                                    TargetKey = propName,
                                    In = "body",
                                    Type = MapSchemaType(prop.Value.Type)
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

        private string SanitizeToolName(string name)
        {
            var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_-]", "_");
            return sanitized.Length > 64 ? sanitized.Substring(0, 64) : sanitized;
        }

        private OpenApiSchema? ResolveSchema(OpenApiSchema? schema, OpenApiDocument document)
        {
            if (schema == null) return null;
            if (schema.Reference != null && document.Components?.Schemas != null)
            {
                if (document.Components.Schemas.TryGetValue(schema.Reference.Id, out var resolved))
                {
                    return resolved;
                }
            }
            return schema;
        }

        private object GetMcpSchema(OpenApiSchema? schema, OpenApiDocument doc, HashSet<string>? visited = null)
        {
            if (schema == null) return new { type = "string" };
            
            visited ??= new HashSet<string>();

            if (schema.Reference != null && !string.IsNullOrEmpty(schema.Reference.Id))
            {
                if (visited.Contains(schema.Reference.Id))
                {
                    return new { type = "object", description = $"Circular reference to {schema.Reference.Id}" };
                }
                visited.Add(schema.Reference.Id);
            }

            var resolved = ResolveSchema(schema, doc);
            if (resolved == null) return new { type = "string" };

            string type = MapSchemaType(resolved.Type);
            
            if (type == "object" && resolved.Properties != null && resolved.Properties.Count > 0)
            {
                var props = new Dictionary<string, object>();
                foreach (var prop in resolved.Properties)
                {
                    props[prop.Key] = GetMcpSchema(prop.Value, doc, new HashSet<string>(visited));
                }
                
                var objSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = props
                };
                
                if (!string.IsNullOrEmpty(resolved.Description))
                    objSchema["description"] = resolved.Description;
                    
                if (resolved.Required != null && resolved.Required.Any())
                    objSchema["required"] = resolved.Required.ToList();
                    
                return objSchema;
            }
            else if (type == "array" && resolved.Items != null)
            {
                var arrSchema = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = GetMcpSchema(resolved.Items, doc, new HashSet<string>(visited))
                };
                
                if (!string.IsNullOrEmpty(resolved.Description))
                    arrSchema["description"] = resolved.Description;
                    
                return arrSchema;
            }

            var basicSchema = new Dictionary<string, object> { ["type"] = type };
            if (!string.IsNullOrEmpty(resolved.Description))
                basicSchema["description"] = resolved.Description;
                
            return basicSchema;
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
