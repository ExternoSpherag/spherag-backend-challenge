namespace ApiLectura.Application.Common;

public static class PagingRules
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public static void Normalize(PagedQuery query)
    {
        if (query.Page < DefaultPage)
            query.Page = DefaultPage;

        if (query.PageSize < 1)
            query.PageSize = DefaultPageSize;

        if (query.PageSize > MaxPageSize)
            query.PageSize = MaxPageSize;
    }
}
