using TeamYellow.Models;

namespace TeamYellow.ViewModels;

public class ClientVM
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public ClientStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
