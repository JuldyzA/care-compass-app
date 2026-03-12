using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.ViewModels;

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

    public async Task<ClientTableDto> GetClientsAsync(string? userId, int page, int pageSize)
    {
         IQueryable<ClientDto> query = _context.Clients
            .Where(c => c.Counsellor.UserId == userId)
            .AsNoTracking()
            .Select(c => new ClientDto
            {
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            });

        int total = await query.CountAsync();

        List<ClientDto> clientsDtos = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ClientTableDto dto = new ClientTableDto
        {
            Clients = clientsDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return dto;
    }
}