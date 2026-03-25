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
        public int TotalCount { get; }
        public int PageSize { get; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class.
        /// </summary>
        /// <param name="items">The items contained in the current page.</param>
        /// <param name="count">The total number of records in the full result set.</param>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="pageSize"/> is less than or equal to zero.
        /// </exception>
        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be greater than zero.");
            }

            TotalCount = count;
            PageSize = pageSize;

            int totalPagesCalculated = (int)Math.Ceiling(count / (double)pageSize);
            TotalPages = Math.Max(1, totalPagesCalculated);

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            if (pageIndex > TotalPages)
            {
                pageIndex = TotalPages;
            }

            PageIndex = pageIndex;

            AddRange(items);
        }

        /// <summary>
        /// Asynchronously creates a paginated list from an IQueryable source.
        /// Executes two queries: one for the total count, one for the current page items.
        /// </summary>
        /// <param name="source">The queryable source to paginate.</param>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A populated paginated list for the requested page.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="pageSize"/> is less than or equal to zero.
        /// </exception>
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
