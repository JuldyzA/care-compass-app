using Microsoft.EntityFrameworkCore;

namespace TeamYellow.Helpers
{
    /// <summary>
    /// Represents a paginated subset of a list, including metadata for navigation.
    /// </summary>
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; }
        public int TotalPages { get; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be greater than zero.");
            }

            PageIndex = pageIndex;

            int totalPagesCalculated = (int)Math.Ceiling(count / (double)pageSize);
            TotalPages = Math.Max(1, totalPagesCalculated);

            AddRange(items);
        }

        /// <summary>
        /// Asynchronously creates a paginated list from an IQueryable source.
        /// Executes two queries: one for the total count, one for the current page items.
        /// </summary>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be greater than zero.");
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            int count = await source.CountAsync();
            int totalPagesCalculated = (int)Math.Ceiling(count / (double)pageSize);
            int totalPages = Math.Max(1, totalPagesCalculated);

            if (pageIndex > totalPages)
            {
                pageIndex = totalPages;
            }

            List<T> items = await source
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
