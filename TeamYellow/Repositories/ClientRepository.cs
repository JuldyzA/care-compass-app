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
    /// <param name="sortColumn">Optional column to sort by ("patient", "datetime", "email").</param>
    /// <param name="sortDir">Optional sort direction ("asc" or "desc").</param>
    /// <returns>A DTO containing the paginated client list and total record metadata.</returns>
    public async Task<ClientTableDto> GetClientsByPageAndFilterAsync
    (
        string? userId,
        int page,
        int pageSize,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? sortColumn = null,
        string? sortDir = null
    ) {

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
            DateTime begin = startDate.Value.Date;
            clients = clients.Where(c => c.CreatedAt >= begin);
        }

        if (endDate.HasValue)
        {
            // treat end date as inclusive by comparing to next day (exclusive)
            DateTime end = endDate.Value.Date.AddDays(1);
            clients = clients.Where(c => c.CreatedAt < end);
        }

        IQueryable<ClientDto> query = clients.Select(c => new ClientDto
        {
            // map ClientId so DTO and later VM receive the identifier
            ClientId = c.ClientId,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            Phone = c.Phone,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        });

        // Determine ordering
        string col = (sortColumn ?? "").ToLower();
        string direction = (sortDir ?? "asc").ToLower();
        direction = direction == "desc" ? "desc" : "asc";

        IQueryable<ClientDto> orderedQuery = col switch
        {
            "patient" => direction == "desc"
                ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),

            "datetime" => direction == "desc"
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),

            "email" => direction == "desc"
                ? query.OrderByDescending(c => c.Email)
                : query.OrderBy(c => c.Email),

            _ => query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
        };

        // Use PaginatedList to get items + metadata
        PaginatedList<ClientDto> paginated = await PaginatedList<ClientDto>.CreateAsync(orderedQuery, page, pageSize);

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

    /// <summary>
    /// Retrieves a specific client by ID if it belongs to the specified counsellor's user.
    /// </summary>
    /// <param name="clientId">The client ID to retrieve.</param>
    /// <param name="userId">The user ID of the counsellor to verify ownership.</param>
    /// <returns>The client entity if found and belongs to the counsellor; otherwise, null.</returns>
    public async Task<Client?> GetClientByIdAsync(int clientId, string userId)
    {
        return await _context.Clients
            .Include(c => c.Counsellor)
            .Where(c => c.ClientId == clientId && c.Counsellor.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Checks if a client with the given email already exists in the database.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>True if a client with this email exists in the database; otherwise, false.</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Clients
            .AsNoTracking()
            .AnyAsync(c => c.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Creates and saves a new client to the database.
    /// </summary>
    /// <param name="client">The client entity to save.</param>
    /// <returns>True if the save was successful; otherwise, false.</returns>
    public async Task<bool> CreateClientAsync(Client client)
    {
        try
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Client created successfully with ID {ClientId} for Counsellor ID {CounsellorId}.", client.ClientId, client.CounsellorId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while creating a client for Counsellor ID {CounsellorId}.", client.CounsellorId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating a client for Counsellor ID {CounsellorId}.", client.CounsellorId);
            return false;
        }
    }

    /// <summary>
    /// Updates an existing client entity in the database.
    /// </summary>
    /// <param name="client">The client entity with updated values to persist.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    public async Task<bool> UpdateClientAsync(Client client)
    {
        try
        {
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Client ID {ClientId} updated successfully.", client.ClientId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating client ID {ClientId}.", client.ClientId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating client ID {ClientId}.", client.ClientId);
            return false;
        }
    }

    /// <summary>
    /// Deletes a specific client by ID if it belongs to the specified counsellor's user.
    /// Performs verification to ensure the client belongs to the authorized counsellor before deletion.
    /// </summary>
    /// <param name="clientId">The client ID to delete.</param>
    /// <param name="userId">The user ID of the counsellor to verify ownership.</param>
    /// <returns>True if the client was successfully deleted; false if the client was not found or does not belong to the counsellor.</returns>
    public async Task<bool> DeleteClientAsync(int clientId, string userId)
    {
        try
        {
            Client? client = await _context.Clients
                .Include(c => c.Counsellor)
                .FirstOrDefaultAsync(c => c.ClientId == clientId && c.Counsellor.UserId == userId);

            if (client == null)
            {
                _logger.LogWarning("Attempted to delete client ID {ClientId} that does not belong to user {UserId}.", clientId, userId);
                return false;
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Client ID {ClientId} deleted successfully for Counsellor with User ID {UserId}.", clientId, userId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting client ID {ClientId} for user {UserId}.", clientId, userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting client ID {ClientId} for user {UserId}.", clientId, userId);
            return false;
        }
    }
}
