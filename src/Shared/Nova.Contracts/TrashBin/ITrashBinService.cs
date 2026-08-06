using Nova.Contracts.CQRS;
using Nova.Contracts.Responses;

namespace Nova.Contracts.TrashBin;

public interface ITrashBinService
{
    /// <summary>
    /// 获取被软删除的数据列表（忽略 EF Core 过滤器）
    /// </summary>
    Task<PagedResult<TrashBinItemDto>> GetDeletedItemsAsync(
        string? entityType = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 一键恢复被软删除的数据
    /// </summary>
    Task<bool> RestoreItemAsync(string entityType, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从数据库物理彻底删除数据
    /// </summary>
    Task<bool> HardDeleteItemAsync(string entityType, Guid id, CancellationToken cancellationToken = default);
}
