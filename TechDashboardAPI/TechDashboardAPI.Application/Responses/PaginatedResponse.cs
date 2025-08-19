namespace TechDashboardAPI.Application.Responses;

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    public PaginatedResponse() { }

    public PaginatedResponse(List<T> data, int totalCount, int page, int pageSize)
    {
        Data = data;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        HasNextPage = page < TotalPages;
        HasPreviousPage = page > 1;
    }

    public static PaginatedResponse<T> Create(List<T> data, int totalCount, int page, int pageSize)
    {
        return new PaginatedResponse<T>(data, totalCount, page, pageSize);
    }
}
