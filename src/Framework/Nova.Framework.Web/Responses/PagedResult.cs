namespace Nova.Framework.Web.Responses;

/// <summary>
/// 通用分页查询结果
/// </summary>
/// <typeparam name="T">列表项的数据类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 符合查询条件的数据总条数
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// 当前页的数据列表
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// 当前请求的页码 (可能为空)
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// 当前请求的页大小 (每页条数，可能为空)
    /// </summary>
    public int? PageSize { get; set; }
    
    /// <summary>
    /// 自动计算的总页数
    /// </summary>
    public int? TotalPages => (PageSize.HasValue && PageSize.Value > 0) 
        ? (int)Math.Ceiling(Total / (double)PageSize.Value) 
        : null;

    /// <summary>
    /// 是否存在上一页
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// 是否存在下一页
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// 当前是否为第一页
    /// </summary>
    public bool IsFirstPage => Page == 1;

    /// <summary>
    /// 当前是否为最后一页
    /// </summary>
    public bool IsLastPage => Page >= TotalPages;
}
