using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services;

public class CounsellorService
{
    private readonly CounsellorRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;

    public CounsellorService(
        CounsellorRepository repository,
        UserManager<IdentityUser> userManager
    ) {
        _repository = repository;
        _userManager = userManager;
    }

    /// <summary>
    /// Retrieves the dashboard data for a specific counsellor, including active subscription status.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the currently logged-in user.</param>
    /// <returns>A view model containing mapped dashboard statistics and user status.</returns>

    public async Task<CounsellorDashboardVM> GetCounsellorDashboardAsync(ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);
        CounsellorDashboardDto dto = await _repository.GetCounsellorDashboardDtoAsync(userId);

        dto.IsSubscriptionActive = dto.CycleEnd > DateTime.UtcNow && dto.Status == SubscriptionStatus.Active;

        CounsellorDashboardVM vm = CounsellorDashboardHelper.MapToVm(dto, userId);

        return vm;
    }

    /// <summary>
    /// Fetches a validated and paginated list of clients for the current user.
    /// </summary>
    /// <param name="user">The current user's claims.</param>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The number of records to return, capped at 100.</param>
    /// <returns>A view model containing the paginated client data.</returns>

    public async Task<ClientTableVm> GetClientsAsync(ClaimsPrincipal user, int page, int pageSize)
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

        ClientTableDto dto = await _repository.GetClientsAsync(userId, page, pageSize);

        ClientTableVm clientTableVm = CounsellorDashboardHelper.MapToVm(dto);

        return clientTableVm;
    }
}