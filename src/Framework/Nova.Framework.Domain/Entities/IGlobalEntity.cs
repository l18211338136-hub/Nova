namespace Nova.Framework.Domain.Entities;

/// <summary>
/// 全局实体标记接口。
/// 用于显式跳过自动多租户隔离。实现了此接口的实体将不会自动追加 TenantId。
/// </summary>
public interface IGlobalEntity
{
}
