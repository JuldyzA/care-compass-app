using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;

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
    /// </summary>
    /// <param name="userId">The unique identifier of the counsellor.</param>
    /// <param name="page">The current page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of client records to include in the result.</param>
    /// <returns>A DTO containing the paginated client list and total record metadata.</returns>
    public async Task<ClientTableDto> GetClientsByPageAsync(string? userId, int page, int pageSize)
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

        // Use PaginatedList to get items + metadata
        var paginated = await PaginatedList<ClientDto>.CreateAsync(query.OrderBy(c => c.FirstName), page, pageSize);

        ClientTableDto dto = new ClientTableDto
        {
            Clients = paginated,
            Page = paginated.PageIndex,
            PageSize = paginated.PageSize,
            TotalCount = paginated.TotalCount
        };

        return dto;
    }
}
