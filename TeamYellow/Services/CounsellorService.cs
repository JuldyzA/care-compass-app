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
}