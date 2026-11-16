using System;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Mcp.Domain.Entities;

/// <summary>
/// MCP 访问密钥的授权范围明细。
/// <para>
/// <c>ToolId == null</c> 表示授予该 <see cref="ServerId"/> 下全部工具（含后续新增工具）；
/// <c>ToolId != null</c> 表示仅授予指定工具。
/// </para>
/// </summary>
public class McpKeyAccess : FullAuditedEntity<Guid>
{
    public Guid McpKeyId { get; private set; }

    public Guid ServerId { get; private set; }

    public Guid? ToolId { get; private set; }

    private McpKeyAccess() { }

    public static McpKeyAccess Create(Guid mcpKeyId, Guid serverId, Guid? toolId = null)
    {
        if (mcpKeyId == Guid.Empty) throw new ArgumentException("McpKeyId cannot be empty.", nameof(mcpKeyId));
        if (serverId == Guid.Empty) throw new ArgumentException("ServerId cannot be empty.", nameof(serverId));

        return new McpKeyAccess
        {
            Id = Guid.CreateVersion7(),
            McpKeyId = mcpKeyId,
            ServerId = serverId,
            ToolId = toolId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
