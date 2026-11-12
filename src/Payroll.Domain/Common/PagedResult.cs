namespace Payroll.Domain.Common;

public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }

    public int TotalPages => PageSize <= 0 ? 0 : Math.Max(1, TotalCount / PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public int FirstItemIndex => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);

    public static PagedResult<T> Empty(int pageSize) => new(Array.Empty<T>(), 0, 1, pageSize);
}

public abstract class PagedQuery
{
    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 10 : value;
    }

    public int Skip => (Page - 1) * PageSize;
}
