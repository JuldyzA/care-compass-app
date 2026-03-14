using TeamYellow.Models;

namespace TeamYellow.Repositories;

public interface ICounsellorRepository
{
    Task<Counsellor?> GetByUserIdAsync(string userId);
    Task<Counsellor> CreateAsync(Counsellor counsellor);
    Task<bool> LicenceIdExistsAsync(string licenceId);
}
