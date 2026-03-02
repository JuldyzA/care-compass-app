using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public interface ICounsellorRepository
{
    Task<CounsellorDashboardDto?> GetCounsellorInfoByUserIdAsync(string userId);
}

public class CounsellorRepository : ICounsellorRepository
{
    private readonly ApplicationDbContext _context;

    public CounsellorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CounsellorDashboardDto> GetCounsellorInfoByUserIdAsync(string userId)
    {
        CounsellorDashboardDto counsellorProfile = new CounsellorDashboardDto();

        return counsellorProfile;
    }
}