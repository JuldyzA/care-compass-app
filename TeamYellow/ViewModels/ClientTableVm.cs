namespace TeamYellow.ViewModels;

public class ClientTableVm
{
    public int Page { get; set; }

    public int StartEntry { get; set; }

    public int EndEntry { get; set; }

    public int TotalCount { get; set; }

    public IEnumerable<ClientVM> Clients { get; set; } = new List<ClientVM>();
}
