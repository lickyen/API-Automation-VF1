namespace ProductivityApi.Models
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages =>
            PageSize > 0
                ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
                : 0;
    }

    public class PaginationRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
    }
}
