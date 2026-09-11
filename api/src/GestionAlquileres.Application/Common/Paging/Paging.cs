namespace GestionAlquileres.Application.Common.Paging;

/// <summary>Clamps client-supplied paging params to safe bounds (audit M10).</summary>
public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize < 1 ? DefaultPageSize : pageSize > MaxPageSize ? MaxPageSize : pageSize;
        return (p, size);
    }
}
