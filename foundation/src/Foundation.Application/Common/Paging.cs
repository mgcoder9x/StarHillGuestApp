namespace Foundation.Application.Common;

/// <summary>Yêu cầu phân trang có chuẩn hóa an toàn (chống page/pageSize xấu).</summary>
public sealed record PagedRequest(int Page = 1, int PageSize = 20)
{
    private const int MaxPageSize = 100;

    public int SafePage => Page < 1 ? 1 : Page;

    public int SafePageSize => PageSize is < 1 or > MaxPageSize ? 20 : PageSize;

    public int Skip => (SafePage - 1) * SafePageSize;
}

/// <summary>Kết quả phân trang bất biến.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public bool HasNext => Page < TotalPages;

    public bool HasPrevious => Page > 1;
}
