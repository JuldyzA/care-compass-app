using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services;

public class ClientService
{
    private readonly ClientRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;

    public ClientService (
        ClientRepository repository,
        UserManager<IdentityUser> userManager
    ) {
        _repository = repository;
        _userManager = userManager;
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
    /// <returns>A view model containing the paginated client data.</returns>
    public async Task<ClientTableVm> GetClientsByPageAsync(ClaimsPrincipal user, int page, int pageSize, string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        string? userId = _userManager.GetUserId(user);

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

        ClientTableDto dto = await _repository.GetClientsByPageAsync(userId, page, pageSize, searchTerm, startDate, endDate);

        int totalPages = (pageSize > 0) ? (int)Math.Ceiling(dto.TotalCount / (double)pageSize) : 1;
        totalPages = Math.Max(1, totalPages);

        if (page > totalPages)
        {
            dto = await _repository.GetClientsByPageAsync(userId, totalPages, pageSize, searchTerm, startDate, endDate);
        }

        ClientTableVm clientTableVm = ClientHelper.MapToVm(dto);

        return clientTableVm;
    }
}
