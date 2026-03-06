using TeamYellow.Models;

namespace TeamYellow.Repositories;

public interface ICounsellorRepository
{
    Task<Counsellor?> GetByUserIdAsync(string userId);
}
