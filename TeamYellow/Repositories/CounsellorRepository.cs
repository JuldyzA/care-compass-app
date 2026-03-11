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
        var data = await (
            from c in _context.Counsellors
            where c.UserId == userId
            join up in _context.UserProfiles on c.UserId equals up.UserId
            select new
            {
                Counsellor = c,
                UserProfile = up,
                // Questionable Cartesian product/cross join
                LatestSubscription = c.Subscriptions
                    .Where(s => s.Status == SubscriptionStatus.Active)
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefault(),
                // Might querying N + 1 time
                Clients = c.Clients
                    .Where(cl => cl.CounsellorId == c.CounsellorId)
                    .ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (data != null)
        {
            CounsellorDashboardDto dto = CounsellorDashboardHelper.MapToDashboardDto(data.Counsellor, data.UserProfile, data.LatestSubscription, data.Clients);
            return dto;
        }

        return null;
    }
}