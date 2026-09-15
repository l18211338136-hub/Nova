using System.Collections.Generic;
using Nova.Modules.Mcp.Domain.ValueObjects;

namespace Nova.Modules.Mcp.Application.Common.Interfaces
{
    public class McpToolDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object InputSchema { get; set; } = new();
        public ExecutionProfile Profile { get; set; } = new();
    }

    public interface IOpenApiToolGenerator
    {
        List<McpToolDefinition> GenerateToolsFromSwagger(string swaggerJson, string baseUrl);
    }
}
