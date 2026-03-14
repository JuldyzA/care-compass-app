using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

/// <summary>
/// Repository providing data access operations for <see cref="Counsellor"/> entities.
/// </summary>
public class CounsellorRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CounsellorRepository> _logger;

    public CounsellorRepository(ApplicationDbContext context, ILogger<CounsellorRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the counsellor profile associated with the specified ASP.NET Identity user ID.
    /// </summary>
    /// <param name="userId">The Identity user ID to search by.</param>
    /// <returns>
    /// The matching <see cref="Counsellor"/> if found; otherwise <c>null</c>.
    /// </returns>
    public async Task<Counsellor?> GetByUserIdAsync(string userId)
    {
        return await _context.Counsellors
        .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    /// <summary>
    /// Persists a new <see cref="Counsellor"/> record to the database.
    /// </summary>
    /// <param name="counsellor">The counsellor entity to add.</param>
    /// <returns>The newly created <see cref="Counsellor"/> with any database-generated values populated.</returns>
    public async Task<Counsellor> CreateAsync(Counsellor counsellor)
    {

        try
        {
            _context.Counsellors.Add(counsellor);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added counsellor '{CounsellorId}'", counsellor.CounsellorId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding counsellor '{PractitionerLicenceId}'", counsellor.PractitionerLicenceId);
        }

        return counsellor;

    }

    /// <summary>
    /// Checks whether a counsellor with the given practitioner licence ID already exists.
    /// Used to ensure uniqueness before assigning a new licence ID.
    /// </summary>
    /// <param name="licenceId">The practitioner licence ID to check.</param>
    /// <returns><c>true</c> if the licence ID is already in use; otherwise <c>false</c>.</returns>
    public async Task<bool> LicenceIdExistsAsync(string licenceId)
    {
        return await _context.Counsellors.AnyAsync(c => c.PractitionerLicenceId == licenceId);
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