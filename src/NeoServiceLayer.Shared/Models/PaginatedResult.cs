using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models
{
    /// <summary>
    /// Represents a paginated result.
    /// </summary>
    /// <typeparam name="T">The type of the items in the result.</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Gets or sets the items in the result.
        /// </summary>
        public IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Gets or sets the pagination information.
        /// </summary>
        public PaginationInfo Pagination { get; set; }

        /// <summary>
        /// Creates a new instance of the PaginatedResult class.
        /// </summary>
        /// <param name="items">The items in the result.</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <param name="page">The current page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A new instance of the PaginatedResult class.</returns>
        public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
        {
            return new PaginatedResult<T>
            {
                Items = items,
                Pagination = new PaginationInfo
                {
                    Total = totalCount,
                    Page = page,
                    Limit = pageSize,
                    Pages = (totalCount + pageSize - 1) / pageSize
                }
            };
        }
    }

    /// <summary>
    /// Represents pagination information.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int Pages { get; set; }
    }
}
