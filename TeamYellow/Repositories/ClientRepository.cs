using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public class ClientRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClientRepository> _logger;

    public ClientRepository(ApplicationDbContext context, ILogger<ClientRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of client data for a specific counsellor, including the total record count for pagination.
    /// Uses PaginatedList helper to produce pagination metadata.
    /// Supports optional searchTerm which filters by first+last name or email, and optional start/end dates (inclusive).
    /// </summary>
    /// <param name="userId">The unique identifier of the counsellor.</param>
    /// <param name="page">The current page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of client records to include in the result.</param>
    /// <param name="searchTerm">Optional search text to filter by name or email.</param>
    /// <param name="startDate">Optional start date (inclusive).</param>
    /// <param name="endDate">Optional end date (inclusive).</param>
    /// <returns>A DTO containing the paginated client list and total record metadata.</returns>
    public async Task<ClientTableDto> GetClientsByPageAsync(string? userId, int page, int pageSize, string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Start from entity query so we can filter before projection (EF Core can translate)
        IQueryable<Client> clients = _context.Clients
            .Include(c => c.Counsellor)
            .Where(c => c.Counsellor.UserId == userId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string q = searchTerm.Trim().ToLower();
            clients = clients.Where(c =>
                (c.FirstName + " " + c.LastName).ToLower().Contains(q) ||
                c.Email.ToLower().Contains(q));
        }

        if (startDate.HasValue)
        {
            var s = startDate.Value.Date;
            clients = clients.Where(c => c.CreatedAt >= s);
        }

        if (endDate.HasValue)
        {
            // treat end date as inclusive by comparing to next day (exclusive)
            var e = endDate.Value.Date.AddDays(1);
            clients = clients.Where(c => c.CreatedAt < e);
        }

        IQueryable<ClientDto> query = clients.Select(c => new ClientDto
        {
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            Phone = c.Phone,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        });

        // Use PaginatedList to get items + metadata
        var paginated = await PaginatedList<ClientDto>.CreateAsync(query.OrderBy(c => c.FirstName), page, pageSize);

        ClientTableDto dto = new ClientTableDto
        {
            Clients = paginated,
            Page = paginated.PageIndex,
            PageSize = paginated.PageSize,
            TotalCount = paginated.TotalCount,
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
            StartDate = startDate,
            EndDate = endDate
        };

        return dto;
    }
}
