namespace LogiMatch.Application.Common.Pagination;

public class PageQuery
{
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 1)
            {
                _pageSize = 20;
                return;
            }

            _pageSize = value > MaxPageSize
                ? MaxPageSize
                : value;
        }
    }
}