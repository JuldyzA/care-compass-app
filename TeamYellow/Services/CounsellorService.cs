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
    private readonly ILogger<CounsellorService> _logger;

    public CounsellorService(
        CounsellorRepository repository,
        UserManager<IdentityUser> userManager,
        ILogger<CounsellorService> logger
    ) {
        _repository = repository;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the dashboard data for a specific counsellor, including active subscription status.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the currently logged-in user.</param>
    /// <returns>A view model containing mapped dashboard statistics and user status.</returns>
    public async Task<CounsellorDashboardVM> GetCounsellorDashboardAsync(ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unable to extract user ID from claims.");
            return new CounsellorDashboardVM();
        }

        CounsellorDashboardDto dto = await _repository.GetCounsellorDashboardDtoAsync(userId);

        dto.IsSubscriptionActive = dto.CycleEnd > DateTime.UtcNow && dto.Status == SubscriptionStatus.Active;

        CounsellorDashboardVM vm = CounsellorDashboardHelper.MapToVm(dto, userId);

        return vm;
    }

    /// <summary>
    /// Retrieves the counsellor information for the authenticated user based on their claims.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the currently logged-in user.</param>
    /// <returns>The Counsellor entity if found; otherwise null.</returns>
    public async Task<Counsellor?> GetCounsellorByUser(ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unable to extract user ID from claims.");
            return null;
        }

        Counsellor? counsellor = await _repository.GetByUserIdAsync(userId);

        if (counsellor == null)
        {
            _logger.LogWarning("No counsellor record found for user ID {UserId}.", userId);
        }

        return counsellor;
    }
}