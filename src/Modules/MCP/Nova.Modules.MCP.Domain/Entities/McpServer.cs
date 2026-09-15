using System;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Mcp.Domain.Entities;

public class McpServer : FullAuditedEntity<Guid>
{
    public string Name { get; private set; } = default!;
    
    public string BaseUrl { get; private set; } = default!;
    
    public string? SwaggerUrl { get; private set; }
    
    public string? AuthToken { get; private set; }

    private McpServer() { }

    public static McpServer Create(string name, string baseUrl, string? swaggerUrl = null, string? authToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        return new McpServer
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            BaseUrl = baseUrl.Trim(),
            SwaggerUrl = swaggerUrl?.Trim(),
            AuthToken = authToken?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string baseUrl, string? swaggerUrl, string? authToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        Name = name.Trim();
        BaseUrl = baseUrl.Trim();
        SwaggerUrl = swaggerUrl?.Trim();
        AuthToken = authToken?.Trim();
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
