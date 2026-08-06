namespace Nova.Contracts.Responses;

public class PagedResult<T>
{
    public long Total { get; set; }
    public IEnumerable<T> Items { get; set; } = [];
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public int? TotalPages => (PageSize.HasValue && PageSize.Value > 0) 
        ? (int)Math.Ceiling(Total / (double)PageSize.Value) 
        : null;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public bool IsFirstPage => Page == 1;
    public bool IsLastPage => Page >= TotalPages;
}
