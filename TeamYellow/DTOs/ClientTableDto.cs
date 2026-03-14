namespace TeamYellow.DTOs;

public class ClientTableDto
{
    public IEnumerable<ClientDto> Clients { get; set; } = new List<ClientDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
