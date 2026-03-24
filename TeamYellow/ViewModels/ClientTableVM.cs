namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing paginated client table data and related UI state.
    /// </summary>
    public class ClientTableVM
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public int StartEntry { get; set; }

        public int EndEntry { get; set; }

        public int TotalCount { get; set; }

        public bool IsDashboard { get; set; }

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;

        public string? SearchTerm { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public IEnumerable<ClientVM> Clients { get; set; } = new List<ClientVM>();
    }
}