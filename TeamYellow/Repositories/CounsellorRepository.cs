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

    /// <summary>
    /// Asynchronously fetches comprehensive dashboard data for a counsellor, including profile details, 
    /// the latest active subscription, and associated client lists.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A populated DTO containing counsellor statistics or an empty DTO if no data is found.</returns>
    public async Task<CounsellorDashboardDto> GetCounsellorDashboardDtoAsync(string? userId)
    {
        var data = await (
            from c in _context.Counsellors
            where c.UserId == userId
            join up in _context.UserProfiles on c.UserId equals up.UserId into ups
            from up in ups.DefaultIfEmpty()
            select new
            {
                Counsellor = c,
                UserProfile = up,
                LatestSubscription = c.Subscriptions
                    .Where(s => s.Status == SubscriptionStatus.Active)
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (data != null)
        {
            var clients = await _context.Clients
                .Where(cl => cl.CounsellorId == data.Counsellor.CounsellorId)
                .AsNoTracking()
                .ToListAsync();

            CounsellorDashboardDto dto = CounsellorDashboardHelper.MapToDashboardDto(
                data.Counsellor,
                data.UserProfile,
                data.LatestSubscription,
                clients);
            return dto;
        }

        return new CounsellorDashboardDto
        {
            DisplayName = string.Empty
        };
    }

    /// <summary>
    /// Retrieves a paginated list of client data for a specific counsellor, including the total record count for pagination.
    /// </summary>
    /// <param name="userId">The unique identifier of the counsellor.</param>
    /// <param name="page">The current page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of client records to include in the result.</param>
    /// <returns>A DTO containing the paginated client list and total record metadata.</returns>
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
            .OrderBy(c => c.FirstName)
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