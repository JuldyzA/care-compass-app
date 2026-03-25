namespace TeamYellow.DTOs
{
    /// <summary>
    /// Represents paginated client table data along with filtering metadata.
    /// </summary>
    public class ClientTableDto
    {
        public IEnumerable<ClientDto> Clients { get; set; } = new List<ClientDto>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public string? SearchTerm { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}