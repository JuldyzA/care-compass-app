using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services
{
    /// <summary>
    /// Service that implements client management business logic for counsellor users.
    /// </summary>
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
        /// <param name="user">The current authenticated user.</param>
        /// <param name="page">The requested page number.</param>
        /// <param name="pageSize">The number of records to return.</param>
        /// <param name="searchTerm">An optional search term to filter clients.</param>
        /// <param name="startDate">An optional inclusive start date filter.</param>
        /// <param name="endDate">An optional inclusive end date filter.</param>
        /// <param name="sortColumn">An optional column to sort by.</param>
        /// <param name="sortDir">An optional sort direction.</param>
        /// <returns>A view model containing paginated client data.</returns>
        public async Task<ClientTableVM> GetClientsByPageAndFilterAsync
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
                return new ClientTableVM();
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

            ClientTableVM clientTableVm = ClientHelper.MapToVm(dto);

            return clientTableVm;
        }

        /// <summary>
        /// Retrieves a specific client if it belongs to the authenticated counsellor.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="user">The current authenticated user.</param>
        /// <returns>The client view model if found; otherwise <c>null</c>.</returns>
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
        /// Creates a new client for the specified counsellor.
        /// </summary>
        /// <param name="vm">The submitted client view model.</param>
        /// <param name="counsellorId">The counsellor identifier to associate with the client.</param>
        /// <returns><c>true</c> if the client was created successfully; otherwise <c>false</c>.</returns>
        public async Task<bool> CreateClientAsync(ClientVM vm, int counsellorId)
        {
            // Check for duplicate email
            bool emailExists = await _repository.EmailExistsAsync(vm.Email);
            if (emailExists)
            {
                _logger.LogWarning("Attempted to create client with duplicate client email {Email}.", vm.Email);
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

        /// <summary>
        /// Updates an existing client if it belongs to the authenticated counsellor.
        /// </summary>
        /// <param name="vm">The submitted client view model containing updated values.</param>
        /// <param name="user">The current authenticated user.</param>
        /// <returns><c>true</c> if the client was updated successfully; otherwise <c>false</c>.</returns>
        public async Task<bool> UpdateClientAsync(ClientVM vm, ClaimsPrincipal user)
        {
            string? userId = _userManager.GetUserId(user);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims for client update.");
                return false;
            }

            // Use GetClientByIdAsync for ownership verification with proper includes for display
            Client? existingClient = await _repository.GetClientByIdAsync(vm.ClientId, userId);
            if (existingClient == null)
            {
                _logger.LogWarning("Client ID {ClientId} not found or does not belong to user {UserId}.", vm.ClientId, userId);
                return false;
            }

            // Check for duplicate email if email was changed
            if (!existingClient.Email.Equals(vm.Email, StringComparison.OrdinalIgnoreCase))
            {
                bool emailExists = await _repository.EmailExistsAsync(vm.Email);
                if (emailExists)
                {
                    _logger.LogWarning("Attempted to update client {ClientId} with duplicate email {Email}.", vm.ClientId, vm.Email);
                    return false;
                }
            }

            // Map the view model to a client entity for update, preserving immutable properties
            Client clientToUpdate = ClientHelper.MapVmToEntityForUpdate(vm, existingClient);

            bool updated = await _repository.UpdateClientAsync(clientToUpdate);

            if (updated)
            {
                _logger.LogInformation("Client {ClientId} updated successfully by user {UserId}.", vm.ClientId, userId);
            }
            else
            {
                _logger.LogError("Failed to update client {ClientId} for user {UserId}.", vm.ClientId, userId);
            }

            return updated;
        }

        /// <summary>
        /// Deletes a specific client if it belongs to the authenticated counsellor.
        /// </summary>
        /// <param name="clientId">The client identifier to delete.</param>
        /// <param name="user">The current authenticated user.</param>
        /// <returns><c>true</c> if the client was deleted successfully; otherwise <c>false</c>.</returns>
        public async Task<bool> DeleteClientAsync(int clientId, ClaimsPrincipal user)
        {
            string? userId = _userManager.GetUserId(user);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims for client deletion.");
                return false;
            }

            bool deleted = await _repository.DeleteClientAsync(clientId, userId);

            if (deleted)
            {
                _logger.LogInformation("Client ID {ClientId} deleted successfully by user {UserId}.", clientId, userId);
            }
            else
            {
                _logger.LogWarning("Failed to delete client ID {ClientId} for user {UserId}.", clientId, userId);
            }

            return deleted;
        }
    }
}