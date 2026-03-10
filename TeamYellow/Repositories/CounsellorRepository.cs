using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public class CounsellorRepository
{
    private readonly ApplicationDbContext _context;

    public CounsellorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CounsellorDashboardDto?> GetCounsellorDashboardDtoAsync(string? userId)
    {
        var data = await _context.Counsellors
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Include(c => c.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
            .Include(c => c.Clients)
            .Join(_context.UserProfiles,
                c => c.UserId,
                u => u.UserId,
                (counsellor, userProfile) => new
                {
                    Counsellor = counsellor,
                    UserProfile = userProfile,
                    LatestSubscription = counsellor.Subscriptions
                        .OrderByDescending(s => s.UpdatedAt)
                        .FirstOrDefault()
                })
            .FirstOrDefaultAsync();

        if (data != null)
        {
            CounsellorDashboardDto dto = CounsellorDashboardHelper.MapToDashboardDto(data.Counsellor, data.UserProfile, data.LatestSubscription);
            return dto;
        }

        return null;
    }
}