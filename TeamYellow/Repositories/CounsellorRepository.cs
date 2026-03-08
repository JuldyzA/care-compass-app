using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public class CounsellorRepository(ApplicationDbContext context) : ICounsellorRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Counsellor?> GetByUserIdAsync(string userId)
    {
        return await _context.Counsellors
        .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Counsellor> CreateAsync(Counsellor counsellor)
    {
        _context.Counsellors.Add(counsellor);
        await _context.SaveChangesAsync();
        return counsellor;
    }

    public async Task<bool> LicenceIdExistsAsync(string licenceId)
    {
        return await _context.Counsellors.AnyAsync(c => c.PractitionerLicenceId == licenceId);
    }
}
