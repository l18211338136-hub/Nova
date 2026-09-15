using System;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Mcp.Domain.Entities;

public class McpTool : FullAuditedEntity<Guid>
{
    public Guid ServerId { get; private set; }
    
    public string Name { get; private set; } = default!;
    
    public string Description { get; private set; } = default!;
    
    public string HttpMethod { get; private set; } = default!;
    
    public string RoutePath { get; private set; } = default!;

    public string InputSchema { get; private set; } = default!;

    public string ParameterMap { get; private set; } = default!;

    public bool IsPublic { get; private set; }
    
    public bool IsEnabled { get; private set; } = true;

    private McpTool() { }

    public static McpTool Create(
        Guid serverId,
        string name,
        string description,
        string httpMethod,
        string routePath,
        string inputSchema,
        string parameterMap,
        bool isPublic = false,
        bool isEnabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);

        return new McpTool
        {
            Id = Guid.CreateVersion7(),
            ServerId = serverId,
            Name = name.Trim(),
            Description = description.Trim(),
            HttpMethod = httpMethod.Trim().ToUpperInvariant(),
            RoutePath = routePath.Trim(),
            InputSchema = inputSchema,
            ParameterMap = parameterMap,
            IsPublic = isPublic,
            IsEnabled = isEnabled,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string description, string inputSchema, string parameterMap, bool isPublic, bool isEnabled)
    {
        Description = description.Trim();
        InputSchema = inputSchema;
        ParameterMap = parameterMap;
        IsPublic = isPublic;
        IsEnabled = isEnabled;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
