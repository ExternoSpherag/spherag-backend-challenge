namespace ApiLectura.Domain.Models;

public class PaginatedResponse<T>
{
    public int Page { get; set; }
    public int SizePage { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public IReadOnlyCollection<T> Items { get; set; } = [];
}