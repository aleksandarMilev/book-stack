namespace BookStack.Common;

public class PaginatedModel<T>(
    IEnumerable<T> items,
    int totalItems,
    int pageIndex,
    int pageSize)
{
    public IEnumerable<T> Items { get; init; } = items;

    public int TotalItems { get; init; } = totalItems;

    public int PageIndex { get; init; } = pageIndex;

    public int PageSize { get; init; } = pageSize;
}
