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
    /// <returns>A view model containing the paginated client data.</returns>

    public async Task<ClientTableVm> GetClientsByPageAsync(ClaimsPrincipal user, int page, int pageSize)
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

        ClientTableDto dto = await _repository.GetClientsByPageAsync(userId, page, pageSize);

        ClientTableVm clientTableVm = ClientHelper.MapToVm(dto);

        return clientTableVm;
    }
}
