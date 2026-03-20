using TeamYellow.Models;

namespace TeamYellow.DTOs;

public class ClientDto
{
    public int ClientId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public ClientStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
