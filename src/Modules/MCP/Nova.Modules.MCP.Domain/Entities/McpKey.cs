using System;
using System.Security.Cryptography;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Mcp.Domain.Entities;

/// <summary>
/// MCP 访问密钥。
/// 用于替代 JWT 登录，供第三方 AI 客户端（如 Claude Desktop）在访问 MCP SSE 端点时鉴权。
/// 密钥以明文存储（业务需求），由服务端一次性生成。
/// </summary>
public class McpKey : FullAuditedEntity<Guid>
{
    /// <summary>密钥名称（便于管理员辨识）。</summary>
    public string Name { get; private set; } = default!;

    /// <summary>密钥明文。生成后入库，对外展示时由前端按需遮罩/复制。</summary>
    public string KeyValue { get; private set; } = default!;

    /// <summary>过期时间。为 null 表示永久有效。</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    private McpKey() { }

    public static McpKey Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new McpKey
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            KeyValue = GenerateKeyValue(),
            ExpiresAt = null, // 业务约定：密钥永久有效
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 仅允许修改名称。密钥本身一旦生成不可变更（需删除重建）。
    /// </summary>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 是否已过期（业务规则：ExpiresAt 为 null 表示永久有效）。
    /// 时间判定属于领域规则，由实体自身负责，避免散落到调用方。
    /// </summary>
    public bool IsExpired(DateTimeOffset utcNow) =>
        ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;

    /// <summary>
    /// 当前是否可用：未被软删除且未过期。
    /// </summary>
    public bool IsActive(DateTimeOffset utcNow) =>
        !IsDeleted && !IsExpired(utcNow);

    /// <summary>
    /// 生成高强度 URL 安全的随机密钥，前缀 mcp_ 便于识别。
    /// </summary>
    private static string GenerateKeyValue()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var token = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"mcp_{token}";
    }
}
