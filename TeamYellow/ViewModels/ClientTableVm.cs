using TeamYellow.DTOs;

namespace TeamYellow.ViewModels;

public class ClientTableVm
{
    public IEnumerable<ClientDto> Clients { get; set; } = new List<ClientDto>();

    public int Page { get; set; }
}
