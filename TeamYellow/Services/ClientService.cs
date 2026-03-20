using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services;

public class ClientService
{
    private readonly ClientRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ClientService> _logger;

    public ClientService(ClientRepository repository, UserManager<IdentityUser> userManager, ILogger<ClientService> logger) 
    {
        _repository = repository;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Fetches a validated and paginated list of clients for the current user.
    /// </summary>
    /// <param name="user">The current user's claims.</param>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The number of records to return, capped at 100.</param>
    /// <param name="searchTerm">Optional search term to filter by name or email.</param>
    /// <param name="startDate">Optional start date (inclusive) to filter client CreatedAt.</param>
    /// <param name="endDate">Optional end date (inclusive) to filter client CreatedAt.</param>
    /// <param name="sortColumn">Optional column to sort by.</param>
    /// <param name="sortDir">Optional sort direction.</param>
    /// <returns>A view model containing the paginated client data.</returns>
    public async Task<ClientTableVm> GetClientsByPageAndFilterAsync
    (
        ClaimsPrincipal user,
        int page,
        int pageSize,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? sortColumn = null,
        string? sortDir = null
    ) {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unable to extract user ID from claims.");
            return new ClientTableVm();
        }

        if (page < 1)
        {
            page = 1;
        }

        const int maxPageSize = 100;
        if (pageSize < 1)
        {
            pageSize = 1;
        }
        else if (pageSize > maxPageSize)
        {
            pageSize = maxPageSize;
        }

        ClientTableDto dto = await _repository.GetClientsByPageAndFilterAsync
        (
            userId,
            page,
            pageSize,
            searchTerm,
            startDate,
            endDate,
            sortColumn,
            sortDir
        );

        int totalPages = (pageSize > 0) ? (int)Math.Ceiling(dto.TotalCount / (double)pageSize) : 1;
        totalPages = Math.Max(1, totalPages);

        if (page > totalPages)
        {
            dto = await _repository.GetClientsByPageAndFilterAsync
            (
                userId,
                totalPages,
                pageSize,
                searchTerm,
                startDate,
                endDate,
                sortColumn,
                sortDir
            );
        }

        ClientTableVm clientTableVm = ClientHelper.MapToVm(dto);

        return clientTableVm;
    }

    /// <summary>
    /// Retrieves a specific client by ID if it belongs to the authenticated counsellor.
    /// </summary>
    /// <param name="clientId">The client ID to retrieve.</param>
    /// <param name="user">The current authenticated user (counsellor).</param>
    /// <returns>The client view model if found and belongs to the counsellor; otherwise, null.</returns>
    public async Task<ClientVM?> GetClientByIdAsync(int clientId, ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unable to extract user ID from claims.");
            return null;
        }

        Client? client = await _repository.GetClientByIdAsync(clientId, userId);

        if (client == null)
        {
            _logger.LogWarning("Client ID {ClientId} not found or does not belong to user {UserId}.", clientId, userId);
            return null;
        }

        return ClientHelper.MapToVm(client);
    }

    /// <summary>
    /// Creates a new client for the authenticated counsellor with duplicate email validation and logging.
    /// </summary>
    /// <param name="vm">The client view model containing the client data.</param>
    /// <param name="user">The current authenticated user (counsellor).</param>
    /// <param name="counsellorId">The counsellor ID to associate with the client.</param>
    /// <returns>True if the client was successfully created; otherwise, false.</returns>
    public async Task<bool> CreateClientAsync(ClientVM vm, ClaimsPrincipal user, int counsellorId)
    {
        // Check for duplicate email
        bool emailExists = await _repository.EmailExistsAsync(vm.Email, counsellorId);
        if (emailExists)
        {
            _logger.LogWarning("Attempted to create client with duplicate email {Email} for Counsellor ID {CounsellorId}.", vm.Email, counsellorId);
            return false;
        }

        Client client = ClientHelper.MapVmToEntity(vm, counsellorId);

        bool saved = await _repository.CreateClientAsync(client);

        if (saved)
        {
            _logger.LogInformation("Client {FirstName} {LastName} created successfully for Counsellor ID {CounsellorId}.", vm.FirstName, vm.LastName, counsellorId);
        }
        else
        {
            _logger.LogError("Failed to create client {FirstName} {LastName} for Counsellor ID {CounsellorId}.", vm.FirstName, vm.LastName, counsellorId);
        }

        return saved;
    }
}
